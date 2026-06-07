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
public class MemberLevelsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMemberLevelService _memberLevelService;

    public MemberLevelsController(ApplicationDbContext context, IMemberLevelService memberLevelService)
    {
        _context = context;
        _memberLevelService = memberLevelService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<MemberLevelDto>>>> GetMemberLevels()
    {
        var levels = await _context.MemberLevels
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.MinPoints)
            .ToListAsync();

        var dtos = levels.Select(l => new MemberLevelDto
        {
            Id = l.Id,
            Name = l.Name,
            MinPoints = l.MinPoints,
            DiscountRate = l.DiscountRate,
            Description = l.Description,
            SortOrder = l.SortOrder,
            IsActive = l.IsActive,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        }).ToList();

        return Ok(ApiResponse.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<MemberLevelDto>>> GetMemberLevel(int id)
    {
        var level = await _context.MemberLevels.FindAsync(id);
        if (level == null)
        {
            return NotFound(ApiResponse.Error<MemberLevelDto>("会员等级不存在"));
        }

        var dto = new MemberLevelDto
        {
            Id = level.Id,
            Name = level.Name,
            MinPoints = level.MinPoints,
            DiscountRate = level.DiscountRate,
            Description = level.Description,
            SortOrder = level.SortOrder,
            IsActive = level.IsActive,
            CreatedAt = level.CreatedAt,
            UpdatedAt = level.UpdatedAt
        };

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<MemberLevelDto>>> CreateMemberLevel([FromBody] CreateMemberLevelDto dto)
    {
        if (await _context.MemberLevels.AnyAsync(l => l.Name == dto.Name))
        {
            return BadRequest(ApiResponse.Error<MemberLevelDto>("等级名称已存在"));
        }

        if (dto.DiscountRate <= 0 || dto.DiscountRate > 1)
        {
            return BadRequest(ApiResponse.Error<MemberLevelDto>("折扣率必须在0到1之间"));
        }

        if (dto.MinPoints < 0)
        {
            return BadRequest(ApiResponse.Error<MemberLevelDto>("最低积分不能为负数"));
        }

        var level = new MemberLevel
        {
            Name = dto.Name,
            MinPoints = dto.MinPoints,
            DiscountRate = dto.DiscountRate,
            Description = dto.Description,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.MemberLevels.Add(level);
        await _context.SaveChangesAsync();

        var result = new MemberLevelDto
        {
            Id = level.Id,
            Name = level.Name,
            MinPoints = level.MinPoints,
            DiscountRate = level.DiscountRate,
            Description = level.Description,
            SortOrder = level.SortOrder,
            IsActive = level.IsActive,
            CreatedAt = level.CreatedAt,
            UpdatedAt = level.UpdatedAt
        };

        return CreatedAtAction(nameof(GetMemberLevel), new { id = level.Id }, ApiResponse.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMemberLevel(int id, [FromBody] UpdateMemberLevelDto dto)
    {
        var level = await _context.MemberLevels.FindAsync(id);
        if (level == null)
        {
            return NotFound(ApiResponse.Error<object>("会员等级不存在"));
        }

        if (await _context.MemberLevels.AnyAsync(l => l.Name == dto.Name && l.Id != id))
        {
            return BadRequest(ApiResponse.Error<object>("等级名称已存在"));
        }

        if (dto.DiscountRate <= 0 || dto.DiscountRate > 1)
        {
            return BadRequest(ApiResponse.Error<object>("折扣率必须在0到1之间"));
        }

        if (dto.MinPoints < 0)
        {
            return BadRequest(ApiResponse.Error<object>("最低积分不能为负数"));
        }

        level.Name = dto.Name;
        level.MinPoints = dto.MinPoints;
        level.DiscountRate = dto.DiscountRate;
        level.Description = dto.Description;
        level.SortOrder = dto.SortOrder;
        level.IsActive = dto.IsActive;
        level.UpdatedAt = DateTime.Now;

        _context.Entry(level).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MemberLevelExists(id))
            {
                return NotFound(ApiResponse.Error<object>("会员等级不存在"));
            }
            else
            {
                throw;
            }
        }

        return Ok(ApiResponse.Ok<object>(new { message = "更新成功" }));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMemberLevel(int id)
    {
        var level = await _context.MemberLevels.FindAsync(id);
        if (level == null)
        {
            return NotFound(ApiResponse.Error<object>("会员等级不存在"));
        }

        _context.MemberLevels.Remove(level);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok<object>(new { message = "删除成功" }));
    }

    [HttpGet("calculate/{totalPoints}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<MemberLevelDto>>> CalculateLevel(int totalPoints)
    {
        var level = await _memberLevelService.GetLevelByTotalPointsAsync(totalPoints);
        if (level == null)
        {
            return NotFound(ApiResponse.Error<MemberLevelDto>("未找到匹配的会员等级"));
        }

        var dto = new MemberLevelDto
        {
            Id = level.Id,
            Name = level.Name,
            MinPoints = level.MinPoints,
            DiscountRate = level.DiscountRate,
            Description = level.Description,
            SortOrder = level.SortOrder,
            IsActive = level.IsActive,
            CreatedAt = level.CreatedAt,
            UpdatedAt = level.UpdatedAt
        };

        return Ok(ApiResponse.Ok(dto));
    }

    private bool MemberLevelExists(int id)
    {
        return _context.MemberLevels.Any(e => e.Id == id);
    }
}
