using System.Security.Claims;
using ClosedXML.Excel;
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
public class PointsRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPointsService _pointsService;

    public PointsRecordsController(ApplicationDbContext context, IPointsService pointsService)
    {
        _context = context;
        _pointsService = pointsService;
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
                ExpireAt = r.ExpireAt,
                AvailablePoints = r.AvailablePoints,
                IsExpired = r.IsExpired,
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

    [HttpGet("export")]
    public async Task<IActionResult> ExportPointsRecords(
        [FromQuery] int? memberUserId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? source = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string format = "xlsx")
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

        var records = await query
            .OrderByDescending(r => r.CreatedAt)
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
                ExpireAt = r.ExpireAt,
                AvailablePoints = r.AvailablePoints,
                IsExpired = r.IsExpired,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        string fileName = $"积分变动记录_{DateTime.Now:yyyyMMddHHmmss}";
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("积分变动记录");

            string[] headers = { "记录ID", "会员ID", "会员账号", "会员昵称", "类型", "积分变动", "变动后余额", "来源", "备注", "订单号", "过期时间", "可用积分", "是否过期", "创建时间" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < records.Count; i++)
            {
                var record = records[i];
                int row = i + 2;

                worksheet.Cell(row, 1).Value = record.Id;
                worksheet.Cell(row, 2).Value = record.MemberUserId;
                worksheet.Cell(row, 3).Value = record.MemberUsername ?? "";
                worksheet.Cell(row, 4).Value = record.MemberNickname ?? "";
                worksheet.Cell(row, 5).Value = record.Type == "Income" ? "收入" : record.Type == "Expense" ? "支出" : record.Type == "Expire" ? "过期" : record.Type;
                worksheet.Cell(row, 6).Value = record.Points;
                worksheet.Cell(row, 7).Value = record.Balance;
                worksheet.Cell(row, 8).Value = record.Source;
                worksheet.Cell(row, 9).Value = record.Remark ?? "";
                worksheet.Cell(row, 10).Value = record.OrderNo ?? "";
                worksheet.Cell(row, 11).Value = record.ExpireAt.HasValue ? record.ExpireAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                worksheet.Cell(row, 12).Value = record.AvailablePoints;
                worksheet.Cell(row, 13).Value = record.IsExpired ? "是" : "否";
                worksheet.Cell(row, 14).Value = record.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            worksheet.Columns().AdjustToContents();
            worksheet.Column(6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Column(7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, contentType, $"{fileName}.xlsx");
            }
        }
    }

    [HttpGet("expiry-summary")]
    public async Task<ActionResult<ApiResponse<PointsExpirySummaryDto>>> GetExpirySummary(
        [FromQuery] int? memberUserId = null)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<PointsExpirySummaryDto>("身份验证失败"));
        }

        var summary = await _pointsService.GetExpirySummaryAsync(targetUserId);
        return Ok(ApiResponse.Ok(summary));
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<ApiResponse<List<ExpiringPointsDto>>>> GetExpiringPoints(
        [FromQuery] int? memberUserId = null,
        [FromQuery] int days = 30)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<List<ExpiringPointsDto>>("身份验证失败"));
        }

        if (days <= 0 || days > 365)
        {
            return BadRequest(ApiResponse.Error<List<ExpiringPointsDto>>("天数必须在1-365之间"));
        }

        var expiringPoints = await _pointsService.GetExpiringPointsAsync(targetUserId, days);
        return Ok(ApiResponse.Ok(expiringPoints));
    }

    [HttpGet("notices")]
    public async Task<ActionResult<ApiResponse<PagedResult<PointsExpiryNoticeDto>>>> GetExpiryNotices(
        [FromQuery] int? memberUserId = null,
        [FromQuery] bool? isRead = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<PagedResult<PointsExpiryNoticeDto>>("身份验证失败"));
        }

        var result = await _pointsService.GetExpiryNoticesAsync(targetUserId, page, pageSize, isRead);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPut("notices/{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkNoticeAsRead(int id, [FromQuery] int? memberUserId = null)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<object>("身份验证失败"));
        }

        try
        {
            await _pointsService.MarkNoticeAsReadAsync(id, targetUserId);
            return Ok(ApiResponse.Ok<object>(new { message = "标记已读成功" }));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse.Error<object>(ex.Message));
        }
    }

    [HttpPut("notices/read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllNoticesAsRead([FromQuery] int? memberUserId = null)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<object>("身份验证失败"));
        }

        await _pointsService.MarkAllNoticesAsReadAsync(targetUserId);
        return Ok(ApiResponse.Ok<object>(new { message = "全部标记已读成功" }));
    }

    [HttpGet("notices/unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadNoticeCount(
        [FromQuery] int? memberUserId = null)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<int>("身份验证失败"));
        }

        var count = await _pointsService.GetUnreadNoticeCountAsync(targetUserId);
        return Ok(ApiResponse.Ok(count));
    }

    private int ValidateAndGetMemberUserId(int requestedUserId)
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        if (roleClaim == null)
        {
            return -1;
        }

        var role = roleClaim.Value;

        if (role == "Admin")
        {
            if (requestedUserId <= 0)
            {
                return -1;
            }
            return requestedUserId;
        }

        if (role == "Member")
        {
            var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdClaim == null || !int.TryParse(nameIdClaim.Value, out var currentUserId))
            {
                return -1;
            }

            if (requestedUserId > 0 && requestedUserId != currentUserId)
            {
                return -1;
            }

            return currentUserId;
        }

        return -1;
    }
}
