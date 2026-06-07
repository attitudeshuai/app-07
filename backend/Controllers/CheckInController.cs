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
        var result = await _checkInService.CheckInAsync(dto.MemberUserId);

        if (!result.Success)
        {
            return BadRequest(ApiResponse.Error<CheckInResultDto>(result.Message));
        }

        return Ok(ApiResponse.Ok(result, result.Message));
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<CheckInStatusDto>>> GetStatus([FromQuery] int memberUserId)
    {
        var status = await _checkInService.GetCheckInStatusAsync(memberUserId);
        return Ok(ApiResponse.Ok(status));
    }

    [HttpGet("rules")]
    public async Task<ActionResult<ApiResponse<List<CheckInRuleDto>>>> GetRules()
    {
        var rules = await _checkInService.GetCheckInRulesAsync();
        return Ok(ApiResponse.Ok(rules));
    }

    [HttpGet("records")]
    public async Task<ActionResult<ApiResponse<PagedResult<CheckInRecordDto>>>> GetRecords(
        [FromQuery] int memberUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _checkInService.GetCheckInRecordsAsync(memberUserId, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }
}
