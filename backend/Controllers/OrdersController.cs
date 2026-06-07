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

    public OrdersController(ApplicationDbContext context, IMemberLevelService memberLevelService, IFlashSaleService flashSaleService, ILogisticsService logisticsService)
    {
        _context = context;
        _memberLevelService = memberLevelService;
        _flashSaleService = flashSaleService;
        _logisticsService = logisticsService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetOrders(
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Orders.AsQueryable();

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
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToListAsync();

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
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error<OrderDto>("订单不存在"));
        }

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
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            OrderHistories = order.OrderHistories.Select(h => new OrderHistoryDto
            {
                Id = h.Id,
                Status = h.Status,
                Remark = h.Remark,
                CreatedAt = h.CreatedAt
            }).ToList()
        };

        if (order.Status == "Shipped" && !string.IsNullOrEmpty(order.TrackingNumber) && !string.IsNullOrEmpty(order.ShippingCompany))
        {
            dto.LogisticsTrace = await _logisticsService.GetLogisticsTraceAsync(order.TrackingNumber, order.ShippingCompany);
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

            if (memberUser.Points < pointsConsumed)
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
                memberUser.Points -= pointsConsumed;
                memberUser.UpdatedAt = DateTime.Now;

                var pointsRecord = new PointsRecord
                {
                    MemberUserId = memberUser.Id,
                    Type = "Expense",
                    Points = pointsConsumed,
                    Balance = memberUser.Points,
                    Source = "Exchange",
                    OrderNo = order.OrderNo,
                    Remark = $"兑换商品: {product.Name}",
                    CreatedAt = DateTime.Now
                };

                _context.PointsRecords.Add(pointsRecord);
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
                UpdatedAt = order.UpdatedAt
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
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
        }

        if (order.Status != "Pending")
        {
            return BadRequest(ApiResponse.Error("订单状态不允许发货"));
        }

        order.Status = "Shipped";
        order.TrackingNumber = dto.TrackingNumber;
        order.ShippingCompany = dto.ShippingCompany;
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
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
        }

        if (order.Status != "Shipped")
        {
            return BadRequest(ApiResponse.Error("订单状态不允许完成"));
        }

        order.Status = "Completed";
        order.UpdatedAt = DateTime.Now;

        order.OrderHistories.Add(new OrderHistory
        {
            Status = "Completed",
            Remark = "订单已完成"
        });

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("订单已完成"));
    }

    [HttpPut("{id}/return")]
    public async Task<ActionResult<ApiResponse<object>>> ReturnOrder(int id, [FromBody] ReturnOrderDto dto)
    {
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound(ApiResponse.Error("订单不存在"));
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
                var memberUser = await _context.MemberUsers.FindAsync(order.MemberUserId.Value);
                if (memberUser != null)
                {
                    memberUser.Points += order.PointsConsumed;
                    memberUser.UpdatedAt = DateTime.Now;

                    var pointsRecord = new PointsRecord
                    {
                        MemberUserId = memberUser.Id,
                        Type = "Income",
                        Points = order.PointsConsumed,
                        Balance = memberUser.Points,
                        Source = "Refund",
                        OrderNo = order.OrderNo,
                        Remark = "退换货，积分退还",
                        CreatedAt = DateTime.Now
                    };

                    _context.PointsRecords.Add(pointsRecord);
                }
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
                var memberUser = await _context.MemberUsers.FindAsync(order.MemberUserId.Value);
                if (memberUser != null)
                {
                    memberUser.Points += order.PointsConsumed;
                    memberUser.UpdatedAt = DateTime.Now;

                    var pointsRecord = new PointsRecord
                    {
                        MemberUserId = memberUser.Id,
                        Type = "Income",
                        Points = order.PointsConsumed,
                        Balance = memberUser.Points,
                        Source = "Refund",
                        OrderNo = order.OrderNo,
                        Remark = "订单取消，积分退还",
                        CreatedAt = DateTime.Now
                    };

                    _context.PointsRecords.Add(pointsRecord);
                }
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
}
