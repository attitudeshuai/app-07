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
public class MemberUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemberLevelService _memberLevelService;

    public MemberUsersController(ApplicationDbContext context, IMemberLevelService memberLevelService)
    {
        _context = context;
        _memberLevelService = memberLevelService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<MemberUserDto>>>> GetMemberUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.MemberUsers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Username.Contains(search) ||
                                      u.Nickname.Contains(search) ||
                                      u.Phone.Contains(search));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(u => u.Status == status);
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();
        var orderCounts = await _context.Orders
            .Where(o => o.MemberUserId.HasValue && userIds.Contains(o.MemberUserId.Value))
            .GroupBy(o => o.MemberUserId)
            .Select(g => new { MemberUserId = g.Key!.Value, Count = g.Count() })
            .ToDictionaryAsync(x => x.MemberUserId, x => x.Count);

        var pointsRecordCounts = await _context.PointsRecords
            .Where(p => userIds.Contains(p.MemberUserId))
            .GroupBy(p => p.MemberUserId)
            .Select(g => new { MemberUserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MemberUserId, x => x.Count);

        var checkInRecordCounts = await _context.CheckInRecords
            .Where(c => userIds.Contains(c.MemberUserId))
            .GroupBy(c => c.MemberUserId)
            .Select(g => new { MemberUserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MemberUserId, x => x.Count);

        var levels = await _memberLevelService.GetActiveLevelsAsync();
        var levelsSorted = levels.OrderByDescending(l => l.MinPoints).ToList();

        var dtos = users.Select(u =>
        {
            var level = levelsSorted.FirstOrDefault(l => l.MinPoints <= u.TotalPoints);
            return new MemberUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Nickname = u.Nickname,
                Phone = u.Phone,
                Email = u.Email,
                Avatar = u.Avatar,
                Points = u.Points,
                TotalPoints = u.TotalPoints,
                ContinuousCheckInDays = u.ContinuousCheckInDays,
                TotalCheckInDays = u.TotalCheckInDays,
                LastCheckInDate = u.LastCheckInDate,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                OrderCount = orderCounts.TryGetValue(u.Id, out var oc) ? oc : 0,
                PointsRecordCount = pointsRecordCounts.TryGetValue(u.Id, out var prc) ? prc : 0,
                CheckInRecordCount = checkInRecordCounts.TryGetValue(u.Id, out var cic) ? cic : 0,
                LevelName = level?.Name ?? string.Empty,
                DiscountRate = level?.DiscountRate ?? 1.0m
            };
        }).ToList();

        var result = new PagedResult<MemberUserDto>
        {
            Items = dtos,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MemberUserDto>>> GetMemberUser(int id)
    {
        var user = await _context.MemberUsers
            .Include(u => u.Orders)
            .Include(u => u.PointsRecords)
            .Include(u => u.CheckInRecords)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return NotFound(ApiResponse.Error<MemberUserDto>("会员用户不存在"));
        }

        var dto = new MemberUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Phone = user.Phone,
            Email = user.Email,
            Avatar = user.Avatar,
            Points = user.Points,
            TotalPoints = user.TotalPoints,
            ContinuousCheckInDays = user.ContinuousCheckInDays,
            TotalCheckInDays = user.TotalCheckInDays,
            LastCheckInDate = user.LastCheckInDate,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            OrderCount = user.Orders.Count,
            PointsRecordCount = user.PointsRecords.Count,
            CheckInRecordCount = user.CheckInRecords.Count,
            LevelName = string.Empty,
            DiscountRate = 1.0m
        };

        var level = await _memberLevelService.GetLevelByTotalPointsAsync(user.TotalPoints);
        if (level != null)
        {
            dto.LevelName = level.Name;
            dto.DiscountRate = level.DiscountRate;
        }

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MemberUserDto>>> CreateMemberUser([FromBody] CreateMemberUserDto dto)
    {
        if (await _context.MemberUsers.AnyAsync(u => u.Username == dto.Username))
        {
            return BadRequest(ApiResponse.Error<MemberUserDto>("用户名已存在"));
        }

        if (await _context.MemberUsers.AnyAsync(u => u.Phone == dto.Phone))
        {
            return BadRequest(ApiResponse.Error<MemberUserDto>("手机号已存在"));
        }

        var user = new MemberUser
        {
            Username = dto.Username,
            Nickname = dto.Nickname,
            Phone = dto.Phone,
            Email = dto.Email,
            Avatar = dto.Avatar,
            Points = dto.Points,
            TotalPoints = dto.Points,
            Status = dto.Status ?? "Active",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        if (dto.Points > 0)
        {
            user.PointsRecords.Add(new PointsRecord
            {
                Type = "Income",
                Points = dto.Points,
                Balance = dto.Points,
                Source = "Admin",
                Remark = "初始积分"
            });
        }

        _context.MemberUsers.Add(user);
        await _context.SaveChangesAsync();

        var result = new MemberUserDto
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Phone = user.Phone,
            Email = user.Email,
            Avatar = user.Avatar,
            Points = user.Points,
            TotalPoints = user.TotalPoints,
            ContinuousCheckInDays = user.ContinuousCheckInDays,
            TotalCheckInDays = user.TotalCheckInDays,
            LastCheckInDate = user.LastCheckInDate,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            OrderCount = 0,
            PointsRecordCount = user.PointsRecords.Count,
            CheckInRecordCount = 0,
            LevelName = string.Empty,
            DiscountRate = 1.0m
        };

        var createLevel = await _memberLevelService.GetLevelByTotalPointsAsync(user.TotalPoints);
        if (createLevel != null)
        {
            result.LevelName = createLevel.Name;
            result.DiscountRate = createLevel.DiscountRate;
        }

        return CreatedAtAction(nameof(GetMemberUser), new { id = user.Id }, ApiResponse.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMemberUser(int id, [FromBody] UpdateMemberUserDto dto)
    {
        var user = await _context.MemberUsers.FindAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse.Error<MemberUserDto>("会员用户不存在"));
        }

        if (!string.IsNullOrEmpty(dto.Nickname))
        {
            user.Nickname = dto.Nickname;
        }
        if (!string.IsNullOrEmpty(dto.Email))
        {
            user.Email = dto.Email;
        }
        if (!string.IsNullOrEmpty(dto.Avatar))
        {
            user.Avatar = dto.Avatar;
        }
        if (!string.IsNullOrEmpty(dto.Status))
        {
            user.Status = dto.Status;
        }
        user.UpdatedAt = DateTime.Now;

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MemberUserExists(id))
            {
                return NotFound(ApiResponse.Error<MemberUserDto>("会员用户不存在"));
            }
            else
            {
                throw;
            }
        }

        return Ok(ApiResponse.Ok<object>(new { message = "更新成功" }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMemberUser(int id)
    {
        var user = await _context.MemberUsers
            .Include(u => u.Orders)
            .Include(u => u.PointsRecords)
            .Include(u => u.CheckInRecords)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return NotFound(ApiResponse.Error<object>("会员用户不存在"));
        }

        if (user.Orders.Any())
        {
            return BadRequest(ApiResponse.Error<object>("该会员存在订单，无法删除"));
        }

        _context.PointsRecords.RemoveRange(user.PointsRecords);
        _context.CheckInRecords.RemoveRange(user.CheckInRecords);
        _context.MemberUsers.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok<object>(new { message = "删除成功" }));
    }

    [HttpPut("{id}/adjust-points")]
    public async Task<ActionResult<ApiResponse<object>>> AdjustPoints(int id, [FromBody] AdjustPointsDto dto)
    {
        if (dto.Points == 0)
        {
            return BadRequest(ApiResponse.Error<object>("积分调整值不能为0"));
        }

        var user = await _context.MemberUsers.FindAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse.Error<object>("会员用户不存在"));
        }

        if (dto.Points < 0 && user.Points + dto.Points < 0)
        {
            return BadRequest(ApiResponse.Error<object>("积分不足，无法扣除"));
        }

        var useTransaction = _context.Database.IsRelational();
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        try
        {
            if (useTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            user.Points += dto.Points;
            if (dto.Points > 0)
            {
                user.TotalPoints += dto.Points;
            }
            user.UpdatedAt = DateTime.Now;

            var record = new PointsRecord
            {
                MemberUserId = id,
                Type = dto.Points > 0 ? "Income" : "Expense",
                Points = Math.Abs(dto.Points),
                Balance = user.Points,
                Source = "Admin",
                Remark = dto.Remark ?? (dto.Points > 0 ? "管理员赠送积分" : "管理员扣除积分"),
                CreatedAt = DateTime.Now
            };

            _context.PointsRecords.Add(record);
            await _context.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            return Ok(ApiResponse.Ok<object>(new { message = "积分调整成功", newBalance = user.Points }));
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

    private bool MemberUserExists(int id)
    {
        return _context.MemberUsers.Any(e => e.Id == id);
    }
}
