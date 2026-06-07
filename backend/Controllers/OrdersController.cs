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
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemberLevelService _memberLevelService;
    private readonly IFlashSaleService _flashSaleService;
    private readonly ILogisticsService _logisticsService;
    private readonly IPointsService _pointsService;
    private readonly IConfiguration _configuration;

    public OrdersController(ApplicationDbContext context, IMemberLevelService memberLevelService, IFlashSaleService flashSaleService, ILogisticsService logisticsService, IPointsService pointsService, IConfiguration configuration)
    {
        _context = context;
        _memberLevelService = memberLevelService;
        _flashSaleService = flashSaleService;
        _logisticsService = logisticsService;
        _pointsService = pointsService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Orders.AsQueryable();

        var memberUserId = GetCurrentMemberUserId();
        var isAdmin = IsCurrentUserAdmin();

        if (!isAdmin && memberUserId.HasValue)
        {
            query = query.Where(o => o.MemberUserId == memberUserId.Value);
        }
        else if (!isAdmin)
        {
            return Unauthorized(ApiResponse.Error<PagedResult<OrderDto>>("身份验证失败"));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(o => o.OrderNo.Contains(search) ||
                                     o.ProductName.Contains(search) ||
                                     o.RecipientName.Contains(search) ||
                                     o.RecipientPhone.Contains(search));
        }

        var autoCompleteDays = _configuration.GetValue<int>("OrderAutoComplete:Days", 7);
        var now = DateTime.Now;

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNo = o.OrderNo,
                OrderType = o.OrderType,
                FlashSaleId = o.FlashSaleId,
                ProductId = o.ProductId,
                ProductName = o.ProductName,
                PointsConsumed = o.PointsConsumed,
                Quantity = o.Quantity,
                RecipientName = o.RecipientName,
                RecipientPhone = o.RecipientPhone,
                RecipientAddress = o.RecipientAddress,
                Status = o.Status,
                TrackingNumber = o.TrackingNumber,
                ShippingCompany = o.ShippingCompany,
                Remark = o.Remark,
                ShippedAt = o.ShippedAt,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                HasReview = _context.ProductReviews.Any(r => r.OrderId == o.Id),
                ReviewId = _context.ProductReviews.Where(r => r.OrderId == o.Id).Select(r => (int?)r.Id).FirstOrDefault(),
                ReviewRating = _context.ProductReviews.Where(r => r.OrderId == o.Id).Select(r => (int?)r.Rating).FirstOrDefault(),
                ReviewContent = _context.ProductReviews.Where(r => r.OrderId == o.Id).Select(r => r.Content).FirstOrDefault(),
                ReviewCreatedAt = _context.ProductReviews.Where(r => r.OrderId == o.Id).Select(r => (DateTime?)r.CreatedAt).FirstOrDefault(),
                MerchantReply = _context.ProductReviews.Where(r => r.OrderId == o.Id).Select(r => r.MerchantReply).FirstOrDefault(),
                MerchantReplyAt = _context.ProductReviews.Where(r => r.OrderId == o.Id).Select(r => (DateTime?)r.MerchantReplyAt).FirstOrDefault()
            })
            .ToListAsync();

        foreach (var order in orders)
        {
            if (order.Status == "Shipped" && order.ShippedAt.HasValue)
            {
                var daysSinceShipped = (int)(now - order.ShippedAt.Value).TotalDays;
                order.AutoCompleteDaysLeft = Math.Max(0, autoCompleteDays - daysSinceShipped);
            }
        }

        var result = new PagedResult<OrderDto>
        {
            Items = orders,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderHistories)
            .Include(o => o.Packages)
                .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error<OrderDto>("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error<OrderDto>("无权查看该订单"));
        }

        var autoCompleteDays = _configuration.GetValue<int>("OrderAutoComplete:Days", 7);

        var review = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.OrderId == order.Id);

        var dto = new OrderDto
        {
            Id = order.Id,
            OrderNo = order.OrderNo,
            OrderType = order.OrderType,
            FlashSaleId = order.FlashSaleId,
            ProductId = order.ProductId,
            ProductName = order.ProductName,
            PointsConsumed = order.PointsConsumed,
            Quantity = order.Quantity,
            RecipientName = order.RecipientName,
            RecipientPhone = order.RecipientPhone,
            RecipientAddress = order.RecipientAddress,
            Status = order.Status,
            TrackingNumber = order.TrackingNumber,
            ShippingCompany = order.ShippingCompany,
            Remark = order.Remark,
            ShippedAt = order.ShippedAt,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            OrderHistories = order.OrderHistories.Select(h => new OrderHistoryDto
            {
                Id = h.Id,
                Status = h.Status,
                Remark = h.Remark,
                CreatedAt = h.CreatedAt
            }).ToList(),
            HasReview = review != null,
            ReviewId = review?.Id,
            ReviewRating = review?.Rating,
            ReviewContent = review?.Content,
            ReviewCreatedAt = review?.CreatedAt,
            MerchantReply = review?.MerchantReply,
            MerchantReplyAt = review?.MerchantReplyAt
        };

        if ((order.Status == "Shipped" || order.Status == "PartiallyShipped") && order.ShippedAt.HasValue)
        {
            var daysSinceShipped = (int)(DateTime.Now - order.ShippedAt.Value).TotalDays;
            dto.AutoCompleteDaysLeft = Math.Max(0, autoCompleteDays - daysSinceShipped);
        }

        if (order.Status == "Shipped" && !string.IsNullOrEmpty(order.TrackingNumber) && !string.IsNullOrEmpty(order.ShippingCompany))
        {
            dto.LogisticsTrace = await _logisticsService.GetLogisticsTraceAsync(order.TrackingNumber, order.ShippingCompany);
        }

        foreach (var package in order.Packages.OrderBy(p => p.CreatedAt))
        {
            var packageDto = new OrderPackageDto
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

            if (package.Status == "Shipped" &&
                !string.IsNullOrEmpty(package.TrackingNumber) &&
                !string.IsNullOrEmpty(package.ShippingCompany))
            {
                packageDto.LogisticsTrace = await _logisticsService.GetLogisticsTraceAsync(
                    package.TrackingNumber, package.ShippingCompany);
            }

            dto.Packages.Add(packageDto);
        }

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpGet("{id}/logistics")]
    public async Task<ActionResult<ApiResponse<LogisticsTraceDto>>> GetOrderLogistics(int id)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error<LogisticsTraceDto>("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error<LogisticsTraceDto>("无权查看该订单的物流信息"));
        }

        if (order.Status != "Shipped")
        {
            return BadRequest(ApiResponse.Error<LogisticsTraceDto>("订单未发货，暂无物流信息"));
        }

        if (string.IsNullOrEmpty(order.TrackingNumber) || string.IsNullOrEmpty(order.ShippingCompany))
        {
            return BadRequest(ApiResponse.Error<LogisticsTraceDto>("缺少运单号或物流公司信息"));
        }

        var trace = await _logisticsService.GetLogisticsTraceAsync(order.TrackingNumber, order.ShippingCompany);

        if (trace == null)
        {
            return BadRequest(ApiResponse.Error<LogisticsTraceDto>("查询物流信息失败"));
        }

        return Ok(ApiResponse.Ok(trace));
    }

    [HttpGet("logistics/companies")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<List<string>>> GetSupportedLogisticsCompanies()
    {
        var companies = _logisticsService.GetSupportedCompanies();
        return Ok(ApiResponse.Ok(companies));
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var product = await _context.Products.FindAsync(dto.ProductId);

        if (product == null)
        {
            return NotFound(ApiResponse.Error<OrderDto>("商品不存在"));
        }

        if (product.Stock < dto.Quantity)
        {
            return BadRequest(ApiResponse.Error<OrderDto>("库存不足"));
        }

        MemberUser? memberUser = null;
        decimal discountRate = 1.0m;
        int basePoints = product.PointsRequired * dto.Quantity;
        int pointsConsumed = basePoints;

        if (dto.MemberUserId.HasValue)
        {
            memberUser = await _context.MemberUsers.FindAsync(dto.MemberUserId.Value);
            if (memberUser == null)
            {
                return BadRequest(ApiResponse.Error<OrderDto>("会员用户不存在"));
            }

            if (memberUser.Status != "Active")
            {
                return BadRequest(ApiResponse.Error<OrderDto>("会员账号已被禁用"));
            }

            discountRate = await _memberLevelService.GetDiscountRateAsync(memberUser.TotalPoints);
            pointsConsumed = (int)Math.Ceiling(basePoints * discountRate);

            var availablePoints = await _pointsService.GetAvailablePointsAsync(memberUser.Id);
            if (availablePoints < pointsConsumed)
            {
                return BadRequest(ApiResponse.Error<OrderDto>("积分不足，无法兑换"));
            }
        }

        var useTransaction = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            var order = new Order
            {
                OrderNo = GenerateOrderNo(),
                ProductId = dto.ProductId,
                ProductName = product.Name,
                PointsConsumed = pointsConsumed,
                Quantity = dto.Quantity,
                RecipientName = dto.RecipientName,
                RecipientPhone = dto.RecipientPhone,
                RecipientAddress = dto.RecipientAddress,
                Status = "Pending",
                Remark = dto.Remark,
                MemberUserId = dto.MemberUserId
            };

            product.Stock -= dto.Quantity;
            product.UpdatedAt = DateTime.Now;

            order.OrderHistories.Add(new OrderHistory
            {
                Status = "Pending",
                Remark = "订单创建成功"
            });

            _context.Orders.Add(order);

            if (memberUser != null)
            {
                var deductDto = new DeductPointsDto
                {
                    MemberUserId = memberUser.Id,
                    Points = pointsConsumed,
                    Source = "Exchange",
                    OrderNo = order.OrderNo,
                    Remark = $"兑换商品: {product.Name}"
                };
                await _pointsService.DeductPointsAsync(deductDto);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            var result = new OrderDto
            {
                Id = order.Id,
                OrderNo = order.OrderNo,
                OrderType = order.OrderType,
                FlashSaleId = order.FlashSaleId,
                ProductId = order.ProductId,
                ProductName = order.ProductName,
                PointsConsumed = order.PointsConsumed,
                Quantity = order.Quantity,
                RecipientName = order.RecipientName,
                RecipientPhone = order.RecipientPhone,
                RecipientAddress = order.RecipientAddress,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                HasReview = false
            };

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, ApiResponse.Ok(result, "订单创建成功"));
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    [HttpPut("{id}/ship")]
    public async Task<ActionResult<ApiResponse<object>>> ShipOrder(int id, [FromBody] ShipOrderDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.Packages)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
        }

        if (!IsCurrentUserAdmin())
        {
            return Unauthorized(ApiResponse.Error("无权执行发货操作"));
        }

        if (order.Status != "Pending")
        {
            return BadRequest(ApiResponse.Error("订单状态不允许发货"));
        }

        var existingPackages = order.Packages.Where(p => p.Status == "Shipped").ToList();
        if (existingPackages.Count > 0)
        {
            return BadRequest(ApiResponse.Error("订单已存在已发货包裹，请使用分批发货接口"));
        }

        var package = new OrderPackage
        {
            OrderId = id,
            PackageNo = GeneratePackageNo(),
            TrackingNumber = dto.TrackingNumber,
            ShippingCompany = dto.ShippingCompany,
            Status = "Shipped",
            Remark = dto.Remark ?? "订单已发货",
            ShippedAt = DateTime.Now,
        };

        package.Items.Add(new OrderPackageItem
        {
            ProductId = order.ProductId,
            ProductName = order.ProductName,
            Quantity = order.Quantity
        });

        _context.OrderPackages.Add(package);

        order.Status = "Shipped";
        order.TrackingNumber = dto.TrackingNumber;
        order.ShippingCompany = dto.ShippingCompany;
        order.ShippedAt = DateTime.Now;
        order.UpdatedAt = DateTime.Now;

        order.OrderHistories.Add(new OrderHistory
        {
            Status = "Shipped",
            Remark = string.IsNullOrEmpty(dto.Remark) ? "订单已发货" : dto.Remark
        });

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("订单发货成功"));
    }

    [HttpPut("{id}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> CompleteOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Packages)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error("无权操作该订单"));
        }

        if (order.Status != "Shipped")
        {
            if (order.Status == "PartiallyShipped")
            {
                return BadRequest(ApiResponse.Error("订单部分发货中，请等待所有包裹发货后再确认收货"));
            }
            return BadRequest(ApiResponse.Error("订单状态不允许完成"));
        }

        var isAdmin = IsCurrentUserAdmin();
        var remark = isAdmin ? "管理员确认收货" : "会员手动确认收货";

        order.Status = "Completed";
        order.UpdatedAt = DateTime.Now;

        order.OrderHistories.Add(new OrderHistory
        {
            Status = "Completed",
            Remark = remark
        });

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("确认收货成功"));
    }

    [HttpPut("{id}/return")]
    public async Task<ActionResult<ApiResponse<object>>> ReturnOrder(int id, [FromBody] ReturnOrderDto dto)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error("无权操作该订单"));
        }

        if (order.Status != "Shipped" && order.Status != "Completed")
        {
            return BadRequest(ApiResponse.Error("订单状态不允许退换货"));
        }

        var useTransaction = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            order.Status = "Returned";
            order.UpdatedAt = DateTime.Now;

            if (order.OrderType == "FlashSale" && order.FlashSaleId.HasValue)
            {
                await _flashSaleService.ReturnStockAsync(order.FlashSaleId.Value, order.Quantity);

                if (order.MemberUserId.HasValue)
                {
                    await _flashSaleService.ReturnUserPurchaseAsync(
                        order.FlashSaleId.Value,
                        order.MemberUserId.Value,
                        order.Quantity);
                }
            }
            else
            {
                var product = await _context.Products.FindAsync(order.ProductId);
                if (product != null)
                {
                    product.Stock += order.Quantity;
                    product.UpdatedAt = DateTime.Now;
                }
            }

            order.OrderHistories.Add(new OrderHistory
            {
                Status = "Returned",
                Remark = $"退换货原因: {dto.Reason}"
            });

            if (order.MemberUserId.HasValue)
            {
                var addDto = new AddPointsDto
                {
                    MemberUserId = order.MemberUserId.Value,
                    Points = order.PointsConsumed,
                    Source = "Refund",
                    OrderNo = order.OrderNo,
                    Remark = "退换货，积分退还"
                };
                await _pointsService.AddPointsAsync(addDto);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return Ok(ApiResponse.Ok("退换货处理成功"));
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> CancelOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
        }

        if (!CanAccessOrder(order))
        {
            return Unauthorized(ApiResponse.Error("无权操作该订单"));
        }

        if (order.Status != "Pending")
        {
            return BadRequest(ApiResponse.Error("订单状态不允许取消"));
        }

        var useTransaction = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.Now;

            if (order.OrderType == "FlashSale" && order.FlashSaleId.HasValue)
            {
                await _flashSaleService.ReturnStockAsync(order.FlashSaleId.Value, order.Quantity);

                if (order.MemberUserId.HasValue)
                {
                    await _flashSaleService.ReturnUserPurchaseAsync(
                        order.FlashSaleId.Value,
                        order.MemberUserId.Value,
                        order.Quantity);
                }
            }
            else
            {
                var product = await _context.Products.FindAsync(order.ProductId);
                if (product != null)
                {
                    product.Stock += order.Quantity;
                    product.UpdatedAt = DateTime.Now;
                }
            }

            order.OrderHistories.Add(new OrderHistory
            {
                Status = "Cancelled",
                Remark = "订单已取消"
            });

            if (order.MemberUserId.HasValue)
            {
                var addDto = new AddPointsDto
                {
                    MemberUserId = order.MemberUserId.Value,
                    Points = order.PointsConsumed,
                    Source = "Refund",
                    OrderNo = order.OrderNo,
                    Remark = "订单取消，积分退还"
                };
                await _pointsService.AddPointsAsync(addDto);
            }

            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return Ok(ApiResponse.Ok("订单取消成功"));
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }
    }

    private string GenerateOrderNo()
    {
        return $"ORD{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
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
