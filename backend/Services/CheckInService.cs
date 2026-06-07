using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public class CheckInService : ICheckInService
{
    private readonly ApplicationDbContext _context;

    public CheckInService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CheckInResultDto> CheckInAsync(int memberUserId)
    {
        var user = await _context.MemberUsers.FindAsync(memberUserId);
        if (user == null)
        {
            return new CheckInResultDto
            {
                Success = false,
                Message = "会员用户不存在"
            };
        }

        if (user.Status != "Active")
        {
            return new CheckInResultDto
            {
                Success = false,
                Message = "会员状态异常，无法签到"
            };
        }

        var today = DateTime.Today;

        if (user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == today)
        {
            return new CheckInResultDto
            {
                Success = false,
                Message = "今日已签到，请明天再来"
            };
        }

        int continuousDays = CalculateContinuousDays(user.LastCheckInDate, user.ContinuousCheckInDays);
        int pointsEarned = GetPointsForContinuousDay(continuousDays);

        var useTransaction = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            user.Points += pointsEarned;
            user.TotalPoints += pointsEarned;
            user.ContinuousCheckInDays = continuousDays;
            user.TotalCheckInDays += 1;
            user.LastCheckInDate = today;
            user.UpdatedAt = DateTime.Now;

            var checkInRecord = new CheckInRecord
            {
                MemberUserId = memberUserId,
                CheckInDate = today,
                PointsEarned = pointsEarned,
                ContinuousDays = continuousDays,
                CreatedAt = DateTime.Now
            };
            _context.CheckInRecords.Add(checkInRecord);

            var pointsRecord = new PointsRecord
            {
                MemberUserId = memberUserId,
                Type = "Income",
                Points = pointsEarned,
                Balance = user.Points,
                Source = "CheckIn",
                Remark = $"连续签到{continuousDays}天，奖励{pointsEarned}积分",
                CreatedAt = DateTime.Now
            };
            _context.PointsRecords.Add(pointsRecord);

            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return new CheckInResultDto
            {
                Success = true,
                PointsEarned = pointsEarned,
                ContinuousDays = continuousDays,
                TotalPoints = user.Points,
                Message = $"签到成功！连续签到{continuousDays}天，获得{pointsEarned}积分"
            };
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

    public async Task<CheckInStatusDto> GetCheckInStatusAsync(int memberUserId)
    {
        var user = await _context.MemberUsers.FindAsync(memberUserId);
        if (user == null)
        {
            return new CheckInStatusDto
            {
                CanCheckIn = false,
                Message = "会员用户不存在"
            };
        }

        var today = DateTime.Today;
        bool canCheckIn = !user.LastCheckInDate.HasValue || user.LastCheckInDate.Value.Date < today;

        int nextDayPoints;
        if (canCheckIn)
        {
            int nextContinuousDays = CalculateContinuousDays(user.LastCheckInDate, user.ContinuousCheckInDays);
            nextDayPoints = GetPointsForContinuousDay(nextContinuousDays);
        }
        else
        {
            nextDayPoints = 0;
        }

        return new CheckInStatusDto
        {
            CanCheckIn = canCheckIn,
            ContinuousDays = user.ContinuousCheckInDays,
            TotalCheckInDays = user.TotalCheckInDays,
            LastCheckInDate = user.LastCheckInDate,
            TodayPoints = nextDayPoints,
            Message = canCheckIn ? "今日可以签到" : "今日已签到"
        };
    }

    public async Task<List<CheckInRuleDto>> GetCheckInRulesAsync()
    {
        var rules = new List<CheckInRuleDto>
        {
            new CheckInRuleDto { Day = 1, Points = 10 },
            new CheckInRuleDto { Day = 2, Points = 20 },
            new CheckInRuleDto { Day = 3, Points = 30 },
            new CheckInRuleDto { Day = 4, Points = 40 },
            new CheckInRuleDto { Day = 5, Points = 50 },
            new CheckInRuleDto { Day = 6, Points = 60 },
            new CheckInRuleDto { Day = 7, Points = 100 }
        };

        return await Task.FromResult(rules);
    }

    public async Task<PagedResult<CheckInRecordDto>> GetCheckInRecordsAsync(int memberUserId, int page, int pageSize)
    {
        var query = _context.CheckInRecords
            .Include(r => r.MemberUser)
            .Where(r => r.MemberUserId == memberUserId)
            .AsQueryable();

        var total = await query.CountAsync();
        var records = await query
            .OrderByDescending(r => r.CheckInDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CheckInRecordDto
            {
                Id = r.Id,
                MemberUserId = r.MemberUserId,
                MemberUsername = r.MemberUser != null ? r.MemberUser.Username : null,
                MemberNickname = r.MemberUser != null ? r.MemberUser.Nickname : null,
                CheckInDate = r.CheckInDate,
                PointsEarned = r.PointsEarned,
                ContinuousDays = r.ContinuousDays,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<CheckInRecordDto>
        {
            Items = records,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public int GetPointsForContinuousDay(int continuousDay)
    {
        return continuousDay switch
        {
            1 => 10,
            2 => 20,
            3 => 30,
            4 => 40,
            5 => 50,
            6 => 60,
            _ => 100
        };
    }

    private int CalculateContinuousDays(DateTime? lastCheckInDate, int currentContinuousDays)
    {
        if (!lastCheckInDate.HasValue)
        {
            return 1;
        }

        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        if (lastCheckInDate.Value.Date == yesterday)
        {
            return currentContinuousDays + 1;
        }
        else if (lastCheckInDate.Value.Date == today)
        {
            return currentContinuousDays;
        }
        else
        {
            return 1;
        }
    }
}
