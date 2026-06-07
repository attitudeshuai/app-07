using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public class FlashSaleReservationService : IFlashSaleReservationService
{
    private readonly ApplicationDbContext _context;

    public FlashSaleReservationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<FlashSaleReservationDto>> CreateReservationAsync(CreateFlashSaleReservationDto dto)
    {
        if (!dto.MemberUserId.HasValue)
        {
            return ApiResponse.Error<FlashSaleReservationDto>("请先登录后再预约");
        }

        var flashSale = await _context.FlashSales
            .Include(f => f.Product)
            .FirstOrDefaultAsync(f => f.Id == dto.FlashSaleId);

        if (flashSale == null)
        {
            return ApiResponse.Error<FlashSaleReservationDto>("秒杀活动不存在");
        }

        if (!flashSale.IsActive)
        {
            return ApiResponse.Error<FlashSaleReservationDto>("秒杀活动已关闭");
        }

        var now = DateTime.Now;
        if (now >= flashSale.StartTime)
        {
            return ApiResponse.Error<FlashSaleReservationDto>("秒杀活动已开始，无法预约");
        }

        var memberUser = await _context.MemberUsers
            .FirstOrDefaultAsync(u => u.Id == dto.MemberUserId.Value);

        if (memberUser == null)
        {
            return ApiResponse.Error<FlashSaleReservationDto>("会员用户不存在");
        }

        if (memberUser.Status != "Active")
        {
            return ApiResponse.Error<FlashSaleReservationDto>("会员账号已被禁用");
        }

        var existing = await _context.FlashSaleReservations
            .FirstOrDefaultAsync(r => r.FlashSaleId == dto.FlashSaleId && r.MemberUserId == dto.MemberUserId.Value);

        if (existing != null)
        {
            return ApiResponse.Error<FlashSaleReservationDto>("您已经预约过该秒杀活动了");
        }

        var reservation = new FlashSaleReservation
        {
            FlashSaleId = dto.FlashSaleId,
            MemberUserId = dto.MemberUserId.Value,
            IsNotified = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.FlashSaleReservations.Add(reservation);

        flashSale.ReservationCount++;
        flashSale.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var result = MapToDto(reservation, flashSale);
        return ApiResponse.Ok(result, "预约成功");
    }

    public async Task<ApiResponse<bool>> CancelReservationAsync(int flashSaleId, int memberUserId)
    {
        var reservation = await _context.FlashSaleReservations
            .FirstOrDefaultAsync(r => r.FlashSaleId == flashSaleId && r.MemberUserId == memberUserId);

        if (reservation == null)
        {
            return ApiResponse.Error<bool>("您还没有预约该秒杀活动");
        }

        var flashSale = await _context.FlashSales.FindAsync(flashSaleId);
        if (flashSale != null)
        {
            flashSale.ReservationCount = Math.Max(0, flashSale.ReservationCount - 1);
            flashSale.UpdatedAt = DateTime.Now;
        }

        _context.FlashSaleReservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok(true, "取消预约成功");
    }

    public async Task<bool> HasReservedAsync(int flashSaleId, int memberUserId)
    {
        return await _context.FlashSaleReservations
            .AnyAsync(r => r.FlashSaleId == flashSaleId && r.MemberUserId == memberUserId);
    }

    public async Task<PagedResult<FlashSaleReservationDto>> GetUserReservationsAsync(FlashSaleReservationQueryDto query)
    {
        var q = _context.FlashSaleReservations
            .Include(r => r.FlashSale!)
            .ThenInclude(f => f.Product)
            .AsQueryable();

        if (query.MemberUserId.HasValue)
        {
            q = q.Where(r => r.MemberUserId == query.MemberUserId.Value);
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => MapToDto(r, r.FlashSale))
            .ToListAsync();

        return new PagedResult<FlashSaleReservationDto>
        {
            Items = items,
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<int> GetReservationCountAsync(int flashSaleId)
    {
        return await _context.FlashSaleReservations
            .CountAsync(r => r.FlashSaleId == flashSaleId);
    }

    public async Task<List<FlashSaleReservation>> GetReservationsForReminderAsync(DateTime startTimeFrom, DateTime startTimeTo)
    {
        return await _context.FlashSaleReservations
            .Include(r => r.FlashSale)
            .Where(r => !r.IsNotified &&
                        r.FlashSale!.IsActive &&
                        r.FlashSale.StartTime >= startTimeFrom &&
                        r.FlashSale.StartTime <= startTimeTo)
            .ToListAsync();
    }

    public async Task MarkAsNotifiedAsync(int reservationId)
    {
        var reservation = await _context.FlashSaleReservations.FindAsync(reservationId);
        if (reservation != null)
        {
            reservation.IsNotified = true;
            reservation.NotifiedAt = DateTime.Now;
            reservation.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ApiResponse<int>> CreateReminderNoticesAsync(int flashSaleId, int minutesBeforeStart)
    {
        var flashSale = await _context.FlashSales.FindAsync(flashSaleId);
        if (flashSale == null)
        {
            return ApiResponse.Error<int>("秒杀活动不存在");
        }

        var reservations = await _context.FlashSaleReservations
            .Where(r => r.FlashSaleId == flashSaleId && !r.IsNotified)
            .ToListAsync();

        if (reservations.Count == 0)
        {
            return ApiResponse.Ok(0, "没有需要通知的预约用户");
        }

        var notices = new List<FlashSaleReminderNotice>();
        foreach (var reservation in reservations)
        {
            notices.Add(new FlashSaleReminderNotice
            {
                MemberUserId = reservation.MemberUserId,
                FlashSaleId = flashSaleId,
                FlashSaleTitle = flashSale.Title,
                ProductName = flashSale.ProductName,
                FlashSalePoints = flashSale.FlashSalePoints,
                StartTime = flashSale.StartTime,
                MinutesBeforeStart = minutesBeforeStart,
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            reservation.IsNotified = true;
            reservation.NotifiedAt = DateTime.Now;
            reservation.UpdatedAt = DateTime.Now;
        }

        _context.FlashSaleReminderNotices.AddRange(notices);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok(notices.Count, $"成功创建 {notices.Count} 条提醒通知");
    }

    public async Task<PagedResult<FlashSaleReminderNoticeDto>> GetUserReminderNoticesAsync(int memberUserId, int page, int pageSize)
    {
        var q = _context.FlashSaleReminderNotices
            .Where(n => n.MemberUserId == memberUserId)
            .AsQueryable();

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new FlashSaleReminderNoticeDto
            {
                Id = n.Id,
                FlashSaleId = n.FlashSaleId,
                FlashSaleTitle = n.FlashSaleTitle,
                ProductName = n.ProductName,
                FlashSalePoints = n.FlashSalePoints,
                StartTime = n.StartTime,
                MinutesBeforeStart = n.MinutesBeforeStart,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<FlashSaleReminderNoticeDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ApiResponse<bool>> MarkNoticeAsReadAsync(int noticeId, int memberUserId)
    {
        var notice = await _context.FlashSaleReminderNotices
            .FirstOrDefaultAsync(n => n.Id == noticeId && n.MemberUserId == memberUserId);

        if (notice == null)
        {
            return ApiResponse.Error<bool>("通知不存在");
        }

        if (notice.IsRead)
        {
            return ApiResponse.Ok(true, "通知已读");
        }

        notice.IsRead = true;
        notice.ReadAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return ApiResponse.Ok(true, "标记已读成功");
    }

    public async Task<int> GetUnreadNoticeCountAsync(int memberUserId)
    {
        return await _context.FlashSaleReminderNotices
            .CountAsync(n => n.MemberUserId == memberUserId && !n.IsRead);
    }

    private static FlashSaleReservationDto MapToDto(FlashSaleReservation reservation, FlashSale? flashSale)
    {
        return new FlashSaleReservationDto
        {
            Id = reservation.Id,
            FlashSaleId = reservation.FlashSaleId,
            FlashSaleTitle = flashSale?.Title ?? string.Empty,
            ProductId = flashSale?.ProductId ?? 0,
            ProductName = flashSale?.ProductName ?? string.Empty,
            FlashSalePoints = flashSale?.FlashSalePoints ?? 0,
            OriginalPoints = flashSale?.Product != null ? flashSale.Product.PointsRequired : flashSale?.FlashSalePoints ?? 0,
            StartTime = flashSale?.StartTime ?? DateTime.MinValue,
            EndTime = flashSale?.EndTime ?? DateTime.MinValue,
            ProductImageUrl = flashSale?.Product?.ImageUrl,
            IsNotified = reservation.IsNotified,
            NotifiedAt = reservation.NotifiedAt,
            CreatedAt = reservation.CreatedAt
        };
    }
}
