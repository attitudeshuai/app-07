using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PointsRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PointsRecordsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PointsRecordDto>>>> GetPointsRecords(
        [FromQuery] int? memberUserId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? source = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.PointsRecords
            .Include(r => r.MemberUser)
            .AsQueryable();

        if (memberUserId.HasValue)
        {
            query = query.Where(r => r.MemberUserId == memberUserId.Value);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(r => r.Type == type);
        }

        if (!string.IsNullOrEmpty(source))
        {
            query = query.Where(r => r.Source == source);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r =>
                r.MemberUser!.Username.Contains(search) ||
                r.MemberUser!.Nickname.Contains(search) ||
                (r.Remark != null && r.Remark.Contains(search)) ||
                (r.OrderNo != null && r.OrderNo.Contains(search)));
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        }

        var total = await query.CountAsync();
        var records = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new PointsRecordDto
            {
                Id = r.Id,
                MemberUserId = r.MemberUserId,
                MemberUsername = r.MemberUser != null ? r.MemberUser.Username : null,
                MemberNickname = r.MemberUser != null ? r.MemberUser.Nickname : null,
                Type = r.Type,
                Points = r.Points,
                Balance = r.Balance,
                Source = r.Source,
                Remark = r.Remark,
                OrderNo = r.OrderNo,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        var result = new PagedResult<PointsRecordDto>
        {
            Items = records,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PointsRecordDto>>> GetPointsRecord(int id)
    {
        var record = await _context.PointsRecords
            .Include(r => r.MemberUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record == null)
        {
            return NotFound(ApiResponse.Error<PointsRecordDto>("积分记录不存在"));
        }

        var dto = new PointsRecordDto
        {
            Id = record.Id,
            MemberUserId = record.MemberUserId,
            MemberUsername = record.MemberUser != null ? record.MemberUser.Username : null,
            MemberNickname = record.MemberUser != null ? record.MemberUser.Nickname : null,
            Type = record.Type,
            Points = record.Points,
            Balance = record.Balance,
            Source = record.Source,
            Remark = record.Remark,
            OrderNo = record.OrderNo,
            CreatedAt = record.CreatedAt
        };

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<object>>> GetSummary(
        [FromQuery] int? memberUserId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = _context.PointsRecords.AsQueryable();

        if (memberUserId.HasValue)
        {
            query = query.Where(r => r.MemberUserId == memberUserId.Value);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(r => r.Type == type);
        }

        if (!string.IsNullOrEmpty(source))
        {
            query = query.Where(r => r.Source == source);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= endDate.Value);
        }

        var totalIncome = await query
            .Where(r => r.Points > 0)
            .SumAsync(r => r.Points);

        var totalExpense = await query
            .Where(r => r.Points < 0)
            .SumAsync(r => Math.Abs(r.Points));

        var count = await query.CountAsync();

        var result = new
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            NetChange = totalIncome - totalExpense,
            RecordCount = count
        };

        return Ok(ApiResponse.Ok(result));
    }
}
