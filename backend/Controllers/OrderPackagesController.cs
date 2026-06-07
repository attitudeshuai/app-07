using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;
using PointsMall.Services;

namespace PointsMall.Controllers;

[ApiController]
[Route("api/orders/{orderId}/packages")]
[Authorize]
public class OrderPackagesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogisticsService _logisticsService;

    public OrderPackagesController(ApplicationDbContext context, ILogisticsService logisticsService)
    {
        _context = context;
        _logisticsService = logisticsService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrderPackageDto>>>> GetPackages(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<List<OrderPackageDto>>("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error<List<OrderPackageDto>>("无权查看该订单的包裹信息"));
        }

        var packages = await _context.OrderPackages
            .Include(p => p.Items)
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        var dtos = new List<OrderPackageDto>();
        foreach (var package in packages)
        {
            var dto = MapToDto(package);

            if (package.Status == "Shipped" &&
                !string.IsNullOrEmpty(package.TrackingNumber) &&
                !string.IsNullOrEmpty(package.ShippingCompany))
            {
                dto.LogisticsTrace = await _logisticsService.GetLogisticsTraceAsync(
                    package.TrackingNumber, package.ShippingCompany);
            }

            dtos.Add(dto);
        }

        return Ok(ApiResponse.Ok(dtos));
    }

    [HttpGet("{packageId}")]
    public async Task<ActionResult<ApiResponse<OrderPackageDto>>> GetPackage(int orderId, int packageId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<OrderPackageDto>("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error<OrderPackageDto>("无权查看该包裹信息"));
        }

        var package = await _context.OrderPackages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == packageId && p.OrderId == orderId);

        if (package == null)
        {
            return NotFound(ApiResponse.Error<OrderPackageDto>("包裹不存在"));
        }

        var dto = MapToDto(package);

        if (package.Status == "Shipped" &&
            !string.IsNullOrEmpty(package.TrackingNumber) &&
            !string.IsNullOrEmpty(package.ShippingCompany))
        {
            dto.LogisticsTrace = await _logisticsService.GetLogisticsTraceAsync(
                package.TrackingNumber, package.ShippingCompany);
        }

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpGet("{packageId}/logistics")]
    public async Task<ActionResult<ApiResponse<LogisticsTraceDto>>> GetPackageLogistics(int orderId, int packageId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<LogisticsTraceDto>("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error<LogisticsTraceDto>("无权查看该包裹的物流信息"));
        }

        var package = await _context.OrderPackages
            .FirstOrDefaultAsync(p => p.Id == packageId && p.OrderId == orderId);

        if (package == null)
        {
            return NotFound(ApiResponse.Error<LogisticsTraceDto>("包裹不存在"));
        }

        if (package.Status != "Shipped")
        {
            return BadRequest(ApiResponse.Error<LogisticsTraceDto>("包裹未发货，暂无物流信息"));
        }

        if (string.IsNullOrEmpty(package.TrackingNumber) || string.IsNullOrEmpty(package.ShippingCompany))
        {
            return BadRequest(ApiResponse.Error<LogisticsTraceDto>("缺少运单号或物流公司信息"));
        }

        var trace = await _logisticsService.GetLogisticsTraceAsync(package.TrackingNumber, package.ShippingCompany);

        if (trace == null)
        {
            return BadRequest(ApiResponse.Error<LogisticsTraceDto>("查询物流信息失败"));
        }

        return Ok(ApiResponse.Ok(trace));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderPackageDto>>> CreatePackage(int orderId, [FromBody] CreateOrderPackageDto dto)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<OrderPackageDto>("订单不存在"));
        }

        if (!IsCurrentUserAdmin())
        {
            return Unauthorized(ApiResponse.Error<OrderPackageDto>("无权创建包裹"));
        }

        if (order.Status == "Cancelled" || order.Status == "Completed" || order.Status == "Returned")
        {
            return BadRequest(ApiResponse.Error<OrderPackageDto>("订单状态不允许创建包裹"));
        }

        if (dto.Items == null || dto.Items.Count == 0)
        {
            return BadRequest(ApiResponse.Error<OrderPackageDto>("至少需要添加一个商品"));
        }

        var existingPackages = await _context.OrderPackages
            .Include(p => p.Items)
            .Where(p => p.OrderId == orderId)
            .ToListAsync();

        var existingQuantities = new Dictionary<int, int>();
        foreach (var pkg in existingPackages)
        {
            foreach (var item in pkg.Items)
            {
                if (existingQuantities.ContainsKey(item.ProductId))
                {
                    existingQuantities[item.ProductId] += item.Quantity;
                }
                else
                {
                    existingQuantities[item.ProductId] = item.Quantity;
                }
            }
        }

        if (!existingQuantities.ContainsKey(order.ProductId))
        {
            existingQuantities[order.ProductId] = 0;
        }

        foreach (var item in dto.Items)
        {
            if (item.ProductId != order.ProductId)
            {
                return BadRequest(ApiResponse.Error<OrderPackageDto>("包裹商品必须与订单商品一致"));
            }

            var totalQty = existingQuantities.GetValueOrDefault(item.ProductId, 0) + item.Quantity;
            if (totalQty > order.Quantity)
            {
                return BadRequest(ApiResponse.Error<OrderPackageDto>($"商品 {item.ProductId} 数量超出订单数量"));
            }
        }

        var package = new OrderPackage
        {
            OrderId = orderId,
            PackageNo = GeneratePackageNo(),
            Status = "Pending",
            Remark = dto.Remark,
        };

        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            package.Items.Add(new OrderPackageItem
            {
                ProductId = item.ProductId,
                ProductName = product?.Name ?? string.Empty,
                Quantity = item.Quantity
            });
        }

        _context.OrderPackages.Add(package);
        await _context.SaveChangesAsync();

        await UpdateOrderStatusAsync(orderId);

        var result = MapToDto(package);
        return CreatedAtAction(nameof(GetPackage), new { orderId, packageId = package.Id }, ApiResponse.Ok(result, "包裹创建成功"));
    }

    [HttpPut("{packageId}")]
    public async Task<ActionResult<ApiResponse<OrderPackageDto>>> UpdatePackage(int orderId, int packageId, [FromBody] UpdateOrderPackageDto dto)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<OrderPackageDto>("订单不存在"));
        }

        if (!IsCurrentUserAdmin())
        {
            return Unauthorized(ApiResponse.Error<OrderPackageDto>("无权修改包裹"));
        }

        var package = await _context.OrderPackages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == packageId && p.OrderId == orderId);

        if (package == null)
        {
            return NotFound(ApiResponse.Error<OrderPackageDto>("包裹不存在"));
        }

        if (package.Status == "Shipped")
        {
            return BadRequest(ApiResponse.Error<OrderPackageDto>("已发货的包裹不能修改"));
        }

        if (dto.TrackingNumber != null)
        {
            package.TrackingNumber = dto.TrackingNumber;
        }
        if (dto.ShippingCompany != null)
        {
            package.ShippingCompany = dto.ShippingCompany;
        }
        if (dto.Remark != null)
        {
            package.Remark = dto.Remark;
        }

        package.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok(MapToDto(package), "包裹更新成功"));
    }

    [HttpDelete("{packageId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePackage(int orderId, int packageId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
        }

        if (!IsCurrentUserAdmin())
        {
            return Unauthorized(ApiResponse.Error("无权删除包裹"));
        }

        var package = await _context.OrderPackages
            .FirstOrDefaultAsync(p => p.Id == packageId && p.OrderId == orderId);

        if (package == null)
        {
            return NotFound(ApiResponse.Error("包裹不存在"));
        }

        if (package.Status == "Shipped")
        {
            return BadRequest(ApiResponse.Error("已发货的包裹不能删除"));
        }

        _context.OrderPackages.Remove(package);
        await _context.SaveChangesAsync();

        await UpdateOrderStatusAsync(orderId);

        return Ok(ApiResponse.Ok("包裹删除成功"));
    }

    [HttpPut("{packageId}/ship")]
    public async Task<ActionResult<ApiResponse<OrderPackageDto>>> ShipPackage(int orderId, int packageId, [FromBody] ShipPackageDto dto)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<OrderPackageDto>("订单不存在"));
        }

        if (!IsCurrentUserAdmin())
        {
            return Unauthorized(ApiResponse.Error<OrderPackageDto>("无权执行发货操作"));
        }

        var package = await _context.OrderPackages
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == packageId && p.OrderId == orderId);

        if (package == null)
        {
            return NotFound(ApiResponse.Error<OrderPackageDto>("包裹不存在"));
        }

        if (package.Status == "Shipped")
        {
            return BadRequest(ApiResponse.Error<OrderPackageDto>("包裹已发货，不能重复发货"));
        }

        if (string.IsNullOrEmpty(dto.TrackingNumber))
        {
            return BadRequest(ApiResponse.Error<OrderPackageDto>("运单号不能为空"));
        }

        if (string.IsNullOrEmpty(dto.ShippingCompany))
        {
            return BadRequest(ApiResponse.Error<OrderPackageDto>("物流公司不能为空"));
        }

        package.Status = "Shipped";
        package.TrackingNumber = dto.TrackingNumber;
        package.ShippingCompany = dto.ShippingCompany;
        package.ShippedAt = DateTime.Now;
        package.UpdatedAt = DateTime.Now;
        if (!string.IsNullOrEmpty(dto.Remark))
        {
            package.Remark = dto.Remark;
        }

        _context.OrderHistories.Add(new OrderHistory
        {
            OrderId = orderId,
            Status = "Shipped",
            Remark = $"包裹 {package.PackageNo} 已发货，物流公司：{dto.ShippingCompany}，运单号：{dto.TrackingNumber}"
        });

        await _context.SaveChangesAsync();

        await UpdateOrderStatusAsync(orderId);

        return Ok(ApiResponse.Ok(MapToDto(package), "包裹发货成功"));
    }

    private async Task UpdateOrderStatusAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Packages)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return;
        }

        if (order.Packages.Count == 0)
        {
            if (order.Status == "PartiallyShipped")
            {
                order.Status = "Pending";
                order.UpdatedAt = DateTime.Now;
            }
        }
        else
        {
            var shippedCount = order.Packages.Count(p => p.Status == "Shipped");
            var totalCount = order.Packages.Count;

            if (shippedCount == 0)
            {
                if (order.Status == "Shipped" || order.Status == "PartiallyShipped")
                {
                    order.Status = "Pending";
                    order.UpdatedAt = DateTime.Now;
                }
            }
            else if (shippedCount < totalCount)
            {
                if (order.Status != "PartiallyShipped")
                {
                    order.Status = "PartiallyShipped";
                    order.UpdatedAt = DateTime.Now;
                    if (!order.ShippedAt.HasValue)
                    {
                        var firstShippedPackage = order.Packages
                            .Where(p => p.Status == "Shipped")
                            .OrderBy(p => p.ShippedAt)
                            .FirstOrDefault();
                        if (firstShippedPackage != null)
                        {
                            order.ShippedAt = firstShippedPackage.ShippedAt;
                        }
                    }

                    if (order.OrderHistories == null)
                    {
                        order.OrderHistories = new List<OrderHistory>();
                    }
                    order.OrderHistories.Add(new OrderHistory
                    {
                        OrderId = orderId,
                        Status = "PartiallyShipped",
                        Remark = "订单部分发货"
                    });
                }
            }
            else
            {
                var prevStatus = order.Status;
                order.Status = "Shipped";
                order.UpdatedAt = DateTime.Now;

                var lastShippedPackage = order.Packages
                    .Where(p => p.Status == "Shipped")
                    .OrderByDescending(p => p.ShippedAt)
                    .FirstOrDefault();
                if (lastShippedPackage != null)
                {
                    order.ShippedAt = lastShippedPackage.ShippedAt;
                    order.TrackingNumber = lastShippedPackage.TrackingNumber;
                    order.ShippingCompany = lastShippedPackage.ShippingCompany;
                }

                if (prevStatus != "Shipped")
                {
                    order.OrderHistories.Add(new OrderHistory
                    {
                        OrderId = orderId,
                        Status = "Shipped",
                        Remark = "所有包裹已发货"
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private static OrderPackageDto MapToDto(OrderPackage package)
    {
        return new OrderPackageDto
        {
            Id = package.Id,
            OrderId = package.OrderId,
            PackageNo = package.PackageNo,
            TrackingNumber = package.TrackingNumber,
            ShippingCompany = package.ShippingCompany,
            Status = package.Status,
            Remark = package.Remark,
            ShippedAt = package.ShippedAt,
            CreatedAt = package.CreatedAt,
            UpdatedAt = package.UpdatedAt,
            Items = package.Items.Select(i => new OrderPackageItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity
            }).ToList()
        };
    }

    private string GeneratePackageNo()
    {
        return $"PKG{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
    }

    private bool IsCurrentUserAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        return roleClaim != null && roleClaim.Value == "Admin";
    }

    private int? GetCurrentMemberUserId()
    {
        var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (nameIdClaim != null && int.TryParse(nameIdClaim.Value, out var userId))
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim != null && roleClaim.Value == "Member")
            {
                return userId;
            }
        }
        return null;
    }

    private bool CanAccessOrder(Order order)
    {
        if (IsCurrentUserAdmin())
        {
            return true;
        }

        var memberUserId = GetCurrentMemberUserId();
        if (memberUserId.HasValue && order.MemberUserId.HasValue && memberUserId.Value == order.MemberUserId.Value)
        {
            return true;
        }

        return false;
    }
}
