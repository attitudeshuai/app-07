using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;
using System.Data;

namespace PointsMall.Services;

public class CheckInService : ICheckInService
{
    private readonly ApplicationDbContext _context;
    private readonly IPointsService _pointsService;

    public CheckInService(ApplicationDbContext context, IPointsService pointsService)
    {
        _context = context;
        _pointsService = pointsService;
    }

    public async Task<CheckInResultDto> CheckInAsync(int memberUserId)
    {
        var today = DateTime.Today;

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

        if (!user.LastLoginDate.HasValue || user.LastLoginDate.Value.Date < today)
        {
            return new CheckInResultDto
            {
                Success = false,
                Message = "请先登录后再进行签到"
            };
        }

        var hasCheckedInToday = await _context.CheckInRecords
            .AnyAsync(r => r.MemberUserId == memberUserId && r.CheckInDate == today);

        if (hasCheckedInToday)
        {
            return new CheckInResultDto
            {
                Success = false,
                Message = "今日已签到，请明天再来"
            };
        }

        try
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    int continuousDays = CalculateContinuousDays(user.LastCheckInDate, user.ContinuousCheckInDays);
                    int pointsEarned = GetPointsForContinuousDay(continuousDays);

                    var addPointsDto = new AddPointsDto
                    {
                        MemberUserId = memberUserId,
                        Points = pointsEarned,
                        Source = "CheckIn",
                        Remark = $"连续签到{continuousDays}天，奖励{pointsEarned}积分"
                    };

                    var pointsRecord = await _pointsService.AddPointsAsync(addPointsDto);

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
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new CheckInResultDto
                    {
                        Success = true,
                        PointsEarned = pointsEarned,
                        ContinuousDays = continuousDays,
                        TotalPoints = pointsRecord.Balance,
                        Message = $"签到成功！连续签到{continuousDays}天，获得{pointsEarned}积分"
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }
        catch (DbUpdateException ex)
        {
            if (ApplicationDbContext.IsUniqueConstraintViolation(ex))
            {
                return new CheckInResultDto
                {
                    Success = false,
                    Message = "今日已签到，请明天再来"
                };
            }

            throw;
        }
        catch (InvalidOperationException ex)
        {
            return new CheckInResultDto
            {
                Success = false,
                Message = ex.Message
            };
        }
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

    public async Task<CheckInStatusDto> GetCheckInStatusAsync(int memberUserId)
    {
        var user = await _context.MemberUsers.FindAsync(memberUserId);
        if (user == null)
        {
            return new CheckInStatusDto
            {
                CanCheckIn = false,
                HasLoggedInToday = false,
                Message = "会员用户不存在"
            };
        }

        var today = DateTime.Today;
        bool hasLoggedInToday = user.LastLoginDate.HasValue && user.LastLoginDate.Value.Date == today;
        bool hasCheckedInToday = user.LastCheckInDate.HasValue && user.LastCheckInDate.Value.Date == today;
        bool canCheckIn = hasLoggedInToday && !hasCheckedInToday;

        int nextDayPoints = 0;
        if (!hasCheckedInToday)
        {
            int nextContinuousDays = CalculateContinuousDays(user.LastCheckInDate, user.ContinuousCheckInDays);
            nextDayPoints = GetPointsForContinuousDay(nextContinuousDays);
        }

        string message;
        if (!hasLoggedInToday)
        {
            message = "请先登录后再签到";
        }
        else if (hasCheckedInToday)
        {
            message = "今日已签到";
        }
        else
        {
            message = "今日可以签到";
        }

        return new CheckInStatusDto
        {
            CanCheckIn = canCheckIn,
            HasLoggedInToday = hasLoggedInToday,
            ContinuousDays = user.ContinuousCheckInDays,
            TotalCheckInDays = user.TotalCheckInDays,
            LastCheckInDate = user.LastCheckInDate,
            LastLoginDate = user.LastLoginDate,
            TodayPoints = nextDayPoints,
            Message = message
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
            new CheckInRuleDto { Day = 7, Points = 70 },
            new CheckInRuleDto { Day = 14, Points = 105 },
            new CheckInRuleDto { Day = 30, Points = 185 },
            new CheckInRuleDto { Day = 60, Points = 275 },
            new CheckInRuleDto { Day = 100, Points = 395 },
            new CheckInRuleDto { Day = 200, Points = 595 },
            new CheckInRuleDto { Day = 365, Points = 925 }
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
        if (continuousDay <= 0)
        {
            return 0;
        }

        int points = 0;

        if (continuousDay <= 7)
        {
            points = continuousDay * 10;
        }
        else if (continuousDay <= 30)
        {
            points = 70 + (continuousDay - 7) * 5;
        }
        else if (continuousDay <= 100)
        {
            points = 185 + (continuousDay - 30) * 3;
        }
        else if (continuousDay <= 365)
        {
            points = 395 + (continuousDay - 100) * 2;
        }
        else
        {
            points = 925 + (continuousDay - 365) * 1;
        }

        const int maxPoints = 1000;
        return Math.Min(points, maxPoints);
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
