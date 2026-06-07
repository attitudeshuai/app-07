using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public class FlashSaleService : IFlashSaleService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemberLevelService _memberLevelService;

    public FlashSaleService(ApplicationDbContext context, IMemberLevelService memberLevelService)
    {
        _context = context;
        _memberLevelService = memberLevelService;
    }

    public async Task<FlashSaleDto?> GetByIdAsync(int id)
    {
        var flashSale = await _context.FlashSales
            .Include(f => f.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flashSale == null) return null;

        return MapToDto(flashSale);
    }

    public async Task<PagedResult<FlashSaleDto>> GetListAsync(FlashSaleQueryDto query)
    {
        var q = _context.FlashSales.AsQueryable();

        if (query.ProductId.HasValue)
        {
            q = q.Where(f => f.ProductId == query.ProductId.Value);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            var now = DateTime.Now;
            switch (query.Status)
            {
                case "NotStarted":
                    q = q.Where(f => f.IsActive && f.StartTime > now);
                    break;
                case "InProgress":
                    q = q.Where(f => f.IsActive && f.StartTime <= now && f.EndTime >= now && f.Stock > 0);
                    break;
                case "Ended":
                    q = q.Where(f => f.IsActive && (f.EndTime < now || f.Stock <= 0));
                    break;
                case "Closed":
                    q = q.Where(f => !f.IsActive);
                    break;
            }
        }

        var total = await q.CountAsync();
        var items = await q
            .Include(f => f.Product)
            .OrderByDescending(f => f.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(f => MapToDto(f))
            .ToListAsync();

        return new PagedResult<FlashSaleDto>
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<List<FlashSaleItemDto>> GetActiveFlashSalesAsync()
    {
        var now = DateTime.Now;
        var flashSales = await _context.FlashSales
            .Include(f => f.Product)
            .Where(f => f.IsActive && f.StartTime <= now && f.EndTime >= now && f.Stock > 0)
            .OrderBy(f => f.EndTime)
            .ToListAsync();

        return flashSales.Select(f => new FlashSaleItemDto
        {
            Id = f.Id,
            Title = f.Title,
            ProductId = f.ProductId,
            ProductName = f.ProductName,
            FlashSalePoints = f.FlashSalePoints,
            OriginalPoints = f.Product != null ? f.Product.PointsRequired : f.FlashSalePoints,
            Stock = f.Stock,
            SoldCount = f.SoldCount,
            ReservationCount = f.ReservationCount,
            StartTime = f.StartTime,
            EndTime = f.EndTime,
            Status = f.GetStatus().ToString(),
            ProductImageUrl = f.Product != null ? f.Product.ImageUrl : null
        }).ToList();
    }

    public async Task<FlashSaleDto?> GetFlashSaleDetailAsync(int id)
    {
        var flashSale = await _context.FlashSales
            .Include(f => f.Product)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (flashSale == null) return null;

        return MapToDto(flashSale);
    }

    public async Task<FlashSaleDto> CreateAsync(CreateFlashSaleDto dto)
    {
        var product = await _context.Products.FindAsync(dto.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("商品不存在");
        }

        if (dto.StartTime >= dto.EndTime)
        {
            throw new InvalidOperationException("开始时间必须早于结束时间");
        }

        if (dto.FlashSalePoints <= 0)
        {
            throw new InvalidOperationException("秒杀积分必须大于0");
        }

        if (dto.FlashSalePoints >= product.PointsRequired)
        {
            throw new InvalidOperationException("秒杀积分必须低于商品正常积分");
        }

        if (dto.Stock <= 0)
        {
            throw new InvalidOperationException("秒杀库存必须大于0");
        }

        var flashSale = new FlashSale
        {
            Title = dto.Title,
            ProductId = dto.ProductId,
            ProductName = product.Name,
            FlashSalePoints = dto.FlashSalePoints,
            Stock = dto.Stock,
            LimitPerUser = dto.LimitPerUser > 0 ? dto.LimitPerUser : 1,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsActive = dto.IsActive
        };

        _context.FlashSales.Add(flashSale);
        await _context.SaveChangesAsync();

        return MapToDto(flashSale);
    }

    public async Task<FlashSaleDto?> UpdateAsync(int id, UpdateFlashSaleDto dto)
    {
        var flashSale = await _context.FlashSales.FindAsync(id);
        if (flashSale == null) return null;

        var product = await _context.Products.FindAsync(flashSale.ProductId);
        if (product == null)
        {
            throw new InvalidOperationException("关联商品不存在");
        }

        if (dto.StartTime >= dto.EndTime)
        {
            throw new InvalidOperationException("开始时间必须早于结束时间");
        }

        if (dto.FlashSalePoints <= 0)
        {
            throw new InvalidOperationException("秒杀积分必须大于0");
        }

        if (dto.FlashSalePoints >= product.PointsRequired)
        {
            throw new InvalidOperationException("秒杀积分必须低于商品正常积分");
        }

        if (dto.Stock < flashSale.SoldCount)
        {
            throw new InvalidOperationException("库存不能小于已售数量");
        }

        flashSale.Title = dto.Title;
        flashSale.FlashSalePoints = dto.FlashSalePoints;
        flashSale.Stock = dto.Stock;
        flashSale.LimitPerUser = dto.LimitPerUser > 0 ? dto.LimitPerUser : 1;
        flashSale.StartTime = dto.StartTime;
        flashSale.EndTime = dto.EndTime;
        flashSale.IsActive = dto.IsActive;
        flashSale.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var flashSale = await _context.FlashSales.FindAsync(id);
        if (flashSale == null) return false;

        _context.FlashSales.Remove(flashSale);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ApiResponse<OrderDto>> CreateFlashSaleOrderAsync(CreateFlashSaleOrderDto dto)
    {
        if (!dto.MemberUserId.HasValue)
        {
            return ApiResponse.Error<OrderDto>("秒杀活动仅限会员用户参与，请先登录");
        }

        if (dto.Quantity <= 0)
        {
            return ApiResponse.Error<OrderDto>("购买数量必须大于0");
        }

        var flashSale = await _context.FlashSales
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == dto.FlashSaleId);

        if (flashSale == null)
        {
            return ApiResponse.Error<OrderDto>("秒杀活动不存在");
        }

        if (!flashSale.IsActive)
        {
            return ApiResponse.Error<OrderDto>("秒杀活动已关闭");
        }

        var now = DateTime.Now;
        if (now < flashSale.StartTime)
        {
            return ApiResponse.Error<OrderDto>("秒杀活动尚未开始");
        }
        if (now > flashSale.EndTime)
        {
            return ApiResponse.Error<OrderDto>("秒杀活动已结束");
        }

        if (dto.Quantity > flashSale.LimitPerUser)
        {
            return ApiResponse.Error<OrderDto>($"每人限购 {flashSale.LimitPerUser} 件");
        }

        var memberUser = await _context.MemberUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == dto.MemberUserId.Value);

        if (memberUser == null)
        {
            return ApiResponse.Error<OrderDto>("会员用户不存在");
        }

        if (memberUser.Status != "Active")
        {
            return ApiResponse.Error<OrderDto>("会员账号已被禁用");
        }

        int pointsConsumed = flashSale.FlashSalePoints * dto.Quantity;

        if (memberUser.Points < pointsConsumed)
        {
            return ApiResponse.Error<OrderDto>("积分不足，无法兑换");
        }

        var useTransaction = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.ReadCommitted);
            }

            var stockRows = await _context.FlashSales
                .Where(f => f.Id == dto.FlashSaleId &&
                            f.IsActive &&
                            f.StartTime <= DateTime.Now &&
                            f.EndTime >= DateTime.Now &&
                            f.Stock >= dto.Quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(f => f.Stock, f => f.Stock - dto.Quantity)
                    .SetProperty(f => f.SoldCount, f => f.SoldCount + dto.Quantity)
                    .SetProperty(f => f.UpdatedAt, f => DateTime.Now));

            if (stockRows == 0)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return ApiResponse.Error<OrderDto>("秒杀库存不足，下手慢啦~");
            }

            var pointsRows = await _context.MemberUsers
                .Where(u => u.Id == dto.MemberUserId.Value &&
                            u.Status == "Active" &&
                            u.Points >= pointsConsumed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.Points, u => u.Points - pointsConsumed)
                    .SetProperty(u => u.UpdatedAt, u => DateTime.Now));

            if (pointsRows == 0)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return ApiResponse.Error<OrderDto>("积分不足，无法兑换");
            }

            int remainingPoints;
            if (useTransaction)
            {
                var latestMember = await _context.MemberUsers
                    .AsNoTracking()
                    .Where(u => u.Id == dto.MemberUserId.Value)
                    .FirstOrDefaultAsync();
                remainingPoints = latestMember?.Points ?? (memberUser.Points - pointsConsumed);

                await _context.Database.ExecuteSqlRawAsync(
                    @"INSERT IGNORE INTO FlashSaleUserPurchases 
                      (FlashSaleId, MemberUserId, Quantity, CreatedAt, UpdatedAt) 
                      VALUES ({0}, {1}, 0, NOW(), NOW())",
                    flashSale.Id, dto.MemberUserId.Value);

                var purchaseRows = await _context.FlashSaleUserPurchases
                    .Where(p => p.FlashSaleId == flashSale.Id &&
                                p.MemberUserId == dto.MemberUserId.Value &&
                                p.Quantity + dto.Quantity <= flashSale.LimitPerUser)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Quantity, p => p.Quantity + dto.Quantity)
                        .SetProperty(p => p.UpdatedAt, p => DateTime.Now));

                if (purchaseRows == 0)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return ApiResponse.Error<OrderDto>($"每人限购 {flashSale.LimitPerUser} 件，您已达购买上限");
                }
            }
            else
            {
                remainingPoints = memberUser.Points - pointsConsumed;

                var purchase = await _context.FlashSaleUserPurchases
                    .FirstOrDefaultAsync(p => p.FlashSaleId == flashSale.Id &&
                                              p.MemberUserId == dto.MemberUserId.Value);

                if (purchase == null)
                {
                    purchase = new FlashSaleUserPurchase
                    {
                        FlashSaleId = flashSale.Id,
                        MemberUserId = dto.MemberUserId.Value,
                        Quantity = 0
                    };
                    _context.FlashSaleUserPurchases.Add(purchase);
                }

                if (purchase.Quantity + dto.Quantity > flashSale.LimitPerUser)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return ApiResponse.Error<OrderDto>($"每人限购 {flashSale.LimitPerUser} 件，您已达购买上限");
                }

                purchase.Quantity += dto.Quantity;
                purchase.UpdatedAt = DateTime.Now;
            }

            var order = new Order
            {
                OrderNo = GenerateFlashSaleOrderNo(),
                OrderType = "FlashSale",
                FlashSaleId = flashSale.Id,
                ProductId = flashSale.ProductId,
                ProductName = flashSale.ProductName,
                PointsConsumed = pointsConsumed,
                Quantity = dto.Quantity,
                RecipientName = dto.RecipientName,
                RecipientPhone = dto.RecipientPhone,
                RecipientAddress = dto.RecipientAddress,
                Status = "Pending",
                Remark = dto.Remark,
                MemberUserId = dto.MemberUserId
            };

            order.OrderHistories.Add(new OrderHistory
            {
                Status = "Pending",
                Remark = "秒杀订单创建成功"
            });

            _context.Orders.Add(order);

            var pointsRecord = new PointsRecord
            {
                MemberUserId = dto.MemberUserId.Value,
                Type = "Expense",
                Points = pointsConsumed,
                Balance = remainingPoints,
                Source = "FlashSale",
                OrderNo = order.OrderNo,
                Remark = $"秒杀兑换: {flashSale.ProductName}",
                CreatedAt = DateTime.Now
            };

            _context.PointsRecords.Add(pointsRecord);

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

            return ApiResponse.Ok(result, "秒杀订单创建成功");
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

    public async Task ReturnStockAsync(int flashSaleId, int quantity)
    {
        var flashSale = await _context.FlashSales.FindAsync(flashSaleId);
        if (flashSale != null)
        {
            flashSale.Stock += quantity;
            flashSale.SoldCount = Math.Max(0, flashSale.SoldCount - quantity);
            flashSale.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ReturnUserPurchaseAsync(int flashSaleId, int memberUserId, int quantity)
    {
        var purchase = await _context.FlashSaleUserPurchases
            .FirstOrDefaultAsync(p => p.FlashSaleId == flashSaleId && p.MemberUserId == memberUserId);

        if (purchase != null)
        {
            purchase.Quantity = Math.Max(0, purchase.Quantity - quantity);
            purchase.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    private FlashSaleDto MapToDto(FlashSale flashSale)
    {
        return new FlashSaleDto
        {
            Id = flashSale.Id,
            Title = flashSale.Title,
            ProductId = flashSale.ProductId,
            ProductName = flashSale.ProductName,
            FlashSalePoints = flashSale.FlashSalePoints,
            OriginalPoints = flashSale.Product != null ? flashSale.Product.PointsRequired : flashSale.FlashSalePoints,
            Stock = flashSale.Stock,
            SoldCount = flashSale.SoldCount,
            ReservationCount = flashSale.ReservationCount,
            LimitPerUser = flashSale.LimitPerUser,
            StartTime = flashSale.StartTime,
            EndTime = flashSale.EndTime,
            IsActive = flashSale.IsActive,
            Status = flashSale.GetStatus().ToString(),
            ProductImageUrl = flashSale.Product != null ? flashSale.Product.ImageUrl : null,
            CreatedAt = flashSale.CreatedAt,
            UpdatedAt = flashSale.UpdatedAt
        };
    }

    private string GenerateFlashSaleOrderNo()
    {
        return $"FS{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
    }
}
