using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;
using System.Data;

namespace PointsMall.Services;

public class PointsService : IPointsService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public PointsService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private int GetPointsValidityDays()
    {
        return _configuration.GetValue<int>("PointsExpiry:ValidityDays", 365);
    }

    public async Task<PointsRecord> AddPointsAsync(AddPointsDto dto)
    {
        if (dto.Points <= 0)
        {
            throw new ArgumentException("积分必须大于0", nameof(dto.Points));
        }

        var hasExistingTransaction = _context.Database.CurrentTransaction != null;
        var useTransaction = _context.Database.IsRelational() && !hasExistingTransaction;
        IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            }

            var user = await LockMemberUserAsync(dto.MemberUserId);
            if (user == null)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw new InvalidOperationException("会员用户不存在");
            }

            if (user.Status != "Active")
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw new InvalidOperationException("会员状态异常");
            }

            DateTime expireAt = dto.ExpireAt ?? DateTime.Now.AddDays(GetPointsValidityDays());

            user.Points += dto.Points;
            user.TotalPoints += dto.Points;
            user.UpdatedAt = DateTime.Now;

            var record = new PointsRecord
            {
                MemberUserId = dto.MemberUserId,
                Type = "Income",
                Points = dto.Points,
                Balance = user.Points,
                Source = dto.Source,
                Remark = dto.Remark,
                OrderNo = dto.OrderNo,
                ExpireAt = expireAt,
                AvailablePoints = dto.Points,
                IsExpired = false,
                CreatedAt = DateTime.Now
            };

            _context.PointsRecords.Add(record);
            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return record;
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

    public async Task<PointsRecord> DeductPointsAsync(DeductPointsDto dto)
    {
        if (dto.Points <= 0)
        {
            throw new ArgumentException("积分必须大于0", nameof(dto.Points));
        }

        var hasExistingTransaction = _context.Database.CurrentTransaction != null;
        var useTransaction = _context.Database.IsRelational() && !hasExistingTransaction;
        IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            }

            var user = await LockMemberUserAsync(dto.MemberUserId);
            if (user == null)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw new InvalidOperationException("会员用户不存在");
            }

            if (user.Status != "Active")
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw new InvalidOperationException("会员状态异常");
            }

            var availableLots = await GetAvailablePointLotsAsync(dto.MemberUserId);
            int totalAvailable = availableLots.Sum(l => l.AvailablePoints);

            if (totalAvailable < dto.Points)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw new InvalidOperationException("可用积分不足");
            }

            int pointsToDeduct = dto.Points;
            foreach (var lot in availableLots)
            {
                if (pointsToDeduct <= 0)
                    break;

                int deductFromLot = Math.Min(lot.AvailablePoints, pointsToDeduct);
                lot.AvailablePoints -= deductFromLot;
                pointsToDeduct -= deductFromLot;
            }

            user.Points -= dto.Points;
            user.UpdatedAt = DateTime.Now;

            var record = new PointsRecord
            {
                MemberUserId = dto.MemberUserId,
                Type = "Expense",
                Points = dto.Points,
                Balance = user.Points,
                Source = dto.Source,
                Remark = dto.Remark,
                OrderNo = dto.OrderNo,
                ExpireAt = null,
                AvailablePoints = 0,
                IsExpired = false,
                CreatedAt = DateTime.Now
            };

            _context.PointsRecords.Add(record);
            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return record;
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

    public async Task<int> GetAvailablePointsAsync(int memberUserId)
    {
        var availableLots = await GetAvailablePointLotsAsync(memberUserId);
        return availableLots.Sum(l => l.AvailablePoints);
    }

    public async Task<PointsExpirySummaryDto> GetExpirySummaryAsync(int memberUserId)
    {
        var user = await _context.MemberUsers.FindAsync(memberUserId);
        if (user == null)
        {
            return new PointsExpirySummaryDto();
        }

        var now = DateTime.Now;
        var in7Days = now.AddDays(7);
        var in30Days = now.AddDays(30);

        var availableLots = await GetAvailablePointLotsAsync(memberUserId);

        var expiringIn7Days = availableLots
            .Where(l => l.ExpireAt.HasValue && l.ExpireAt.Value <= in7Days)
            .Sum(l => l.AvailablePoints);

        var expiringIn30Days = availableLots
            .Where(l => l.ExpireAt.HasValue && l.ExpireAt.Value <= in30Days)
            .Sum(l => l.AvailablePoints);

        var nextLot = availableLots
            .Where(l => l.ExpireAt.HasValue)
            .OrderBy(l => l.ExpireAt)
            .FirstOrDefault();

        var total = availableLots.Sum(l => l.AvailablePoints);

        return new PointsExpirySummaryDto
        {
            TotalPoints = total,
            ExpiringIn7Days = expiringIn7Days,
            ExpiringIn30Days = expiringIn30Days,
            NextExpireDate = nextLot?.ExpireAt,
            NextExpirePoints = nextLot?.AvailablePoints ?? 0
        };
    }

    public async Task<List<ExpiringPointsDto>> GetExpiringPointsAsync(int memberUserId, int days)
    {
        var now = DateTime.Now;
        var targetDate = now.AddDays(days);

        var availableLots = await GetAvailablePointLotsAsync(memberUserId);

        var expiringLots = availableLots
            .Where(l => l.ExpireAt.HasValue && l.ExpireAt.Value <= targetDate)
            .ToList();

        var result = expiringLots
            .GroupBy(l => l.ExpireAt!.Value.Date)
            .Select(g => new ExpiringPointsDto
            {
                Points = g.Sum(l => l.AvailablePoints),
                ExpireDate = g.Key,
                DaysUntilExpiry = (g.Key - now.Date).Days
            })
            .OrderBy(e => e.ExpireDate)
            .ToList();

        return result;
    }

    public async Task<List<PointsRecord>> GetAvailablePointLotsAsync(int memberUserId)
    {
        var now = DateTime.Now;

        var lots = await _context.PointsRecords
            .Where(r => r.MemberUserId == memberUserId
                && r.Type == "Income"
                && !r.IsExpired
                && r.AvailablePoints > 0
                && (r.ExpireAt == null || r.ExpireAt > now))
            .OrderBy(r => r.ExpireAt ?? DateTime.MaxValue)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync();

        return lots;
    }

    public async Task<int> ProcessExpiredPointsAsync()
    {
        var now = DateTime.Now;
        var expiredLots = await _context.PointsRecords
            .Where(r => r.Type == "Income"
                && !r.IsExpired
                && r.AvailablePoints > 0
                && r.ExpireAt.HasValue
                && r.ExpireAt.Value <= now)
            .ToListAsync();

        if (!expiredLots.Any())
        {
            return 0;
        }

        var userGroups = expiredLots.GroupBy(l => l.MemberUserId);
        int expiredCount = 0;

        foreach (var group in userGroups)
        {
            var userId = group.Key;
            var userExpiredLots = group.ToList();
            var totalExpiredPoints = userExpiredLots.Sum(l => l.AvailablePoints);

            var user = await _context.MemberUsers.FindAsync(userId);
            if (user == null)
            {
                continue;
            }

            foreach (var lot in userExpiredLots)
            {
                lot.IsExpired = true;
                lot.AvailablePoints = 0;
                expiredCount++;
            }

            user.Points = Math.Max(0, user.Points - totalExpiredPoints);
            user.UpdatedAt = DateTime.Now;

            var expireRecord = new PointsRecord
            {
                MemberUserId = userId,
                Type = "Expire",
                Points = totalExpiredPoints,
                Balance = user.Points,
                Source = "System",
                Remark = $"积分过期，共{totalExpiredPoints}积分失效",
                ExpireAt = null,
                AvailablePoints = 0,
                IsExpired = false,
                CreatedAt = DateTime.Now
            };

            _context.PointsRecords.Add(expireRecord);
        }

        await _context.SaveChangesAsync();
        return expiredCount;
    }

    public async Task<int> GenerateExpiryNoticesAsync(int daysBeforeExpiry)
    {
        var now = DateTime.Now;
        var targetDate = now.AddDays(daysBeforeExpiry);
        var targetDateEnd = targetDate.Date.AddDays(1).AddTicks(-1);

        var expiringLots = await _context.PointsRecords
            .Where(r => r.Type == "Income"
                && !r.IsExpired
                && r.AvailablePoints > 0
                && r.ExpireAt.HasValue
                && r.ExpireAt.Value >= targetDate.Date
                && r.ExpireAt.Value <= targetDateEnd)
            .ToListAsync();

        if (!expiringLots.Any())
        {
            return 0;
        }

        var userGroups = expiringLots.GroupBy(l => l.MemberUserId);
        int noticeCount = 0;

        foreach (var group in userGroups)
        {
            var userId = group.Key;
            var totalExpiringPoints = group.Sum(l => l.AvailablePoints);
            var expireDate = group.Min(l => l.ExpireAt!.Value);

            var existingNotice = await _context.PointsExpiryNotices
                .AnyAsync(n => n.MemberUserId == userId
                    && n.DaysBeforeExpiry == daysBeforeExpiry
                    && n.ExpireDate.Date == expireDate.Date);

            if (existingNotice)
            {
                continue;
            }

            var notice = new PointsExpiryNotice
            {
                MemberUserId = userId,
                PointsExpiring = totalExpiringPoints,
                ExpireDate = expireDate,
                DaysBeforeExpiry = daysBeforeExpiry,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.PointsExpiryNotices.Add(notice);
            noticeCount++;
        }

        await _context.SaveChangesAsync();
        return noticeCount;
    }

    public async Task<PagedResult<PointsExpiryNoticeDto>> GetExpiryNoticesAsync(
        int memberUserId, int page, int pageSize, bool? isRead = null)
    {
        var query = _context.PointsExpiryNotices
            .Include(n => n.MemberUser)
            .Where(n => n.MemberUserId == memberUserId)
            .AsQueryable();

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var total = await query.CountAsync();
        var notices = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new PointsExpiryNoticeDto
            {
                Id = n.Id,
                MemberUserId = n.MemberUserId,
                MemberUsername = n.MemberUser != null ? n.MemberUser.Username : null,
                MemberNickname = n.MemberUser != null ? n.MemberUser.Nickname : null,
                PointsExpiring = n.PointsExpiring,
                ExpireDate = n.ExpireDate,
                DaysBeforeExpiry = n.DaysBeforeExpiry,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<PointsExpiryNoticeDto>
        {
            Items = notices,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task MarkNoticeAsReadAsync(int noticeId, int memberUserId)
    {
        var notice = await _context.PointsExpiryNotices
            .FirstOrDefaultAsync(n => n.Id == noticeId && n.MemberUserId == memberUserId);

        if (notice == null)
        {
            throw new InvalidOperationException("通知不存在");
        }

        if (!notice.IsRead)
        {
            notice.IsRead = true;
            notice.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllNoticesAsReadAsync(int memberUserId)
    {
        var unreadNotices = await _context.PointsExpiryNotices
            .Where(n => n.MemberUserId == memberUserId && !n.IsRead)
            .ToListAsync();

        foreach (var notice in unreadNotices)
        {
            notice.IsRead = true;
            notice.ReadAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadNoticeCountAsync(int memberUserId)
    {
        return await _context.PointsExpiryNotices
            .CountAsync(n => n.MemberUserId == memberUserId && !n.IsRead);
    }

    private async Task<MemberUser?> LockMemberUserAsync(int memberUserId)
    {
        if (_context.Database.IsMySql())
        {
            var users = await _context.MemberUsers
                .FromSqlInterpolated($"SELECT * FROM MemberUsers WHERE Id = {memberUserId} FOR UPDATE")
                .ToListAsync();
            return users.FirstOrDefault();
        }

        var user = await _context.MemberUsers.FindAsync(memberUserId);
        return user;
    }
}
