using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PointsMall.Dtos;
using PointsMall.Services;

namespace PointsMall.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CheckInController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    [HttpPost("do")]
    public async Task<ActionResult<ApiResponse<CheckInResultDto>>> DoCheckIn([FromBody] DoCheckInDto dto)
    {
        var memberUserId = ValidateAndGetMemberUserId(dto.MemberUserId);
        if (memberUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<CheckInResultDto>("身份验证失败"));
        }

        var result = await _checkInService.CheckInAsync(memberUserId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.Error<CheckInResultDto>(result.Message));
        }

        return Ok(ApiResponse.Ok(result, result.Message));
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<CheckInStatusDto>>> GetStatus([FromQuery] int? memberUserId)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<CheckInStatusDto>("身份验证失败"));
        }

        var status = await _checkInService.GetCheckInStatusAsync(targetUserId);
        return Ok(ApiResponse.Ok(status));
    }

    [HttpGet("rules")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CheckInRuleDto>>>> GetRules()
    {
        var rules = await _checkInService.GetCheckInRulesAsync();
        return Ok(ApiResponse.Ok(rules));
    }

    [HttpGet("records")]
    public async Task<ActionResult<ApiResponse<PagedResult<CheckInRecordDto>>>> GetRecords(
        [FromQuery] int? memberUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        int targetUserId = ValidateAndGetMemberUserId(memberUserId ?? 0);
        if (targetUserId <= 0)
        {
            return Unauthorized(ApiResponse.Error<PagedResult<CheckInRecordDto>>("身份验证失败"));
        }

        var result = await _checkInService.GetCheckInRecordsAsync(targetUserId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
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
