using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PointsMall.Dtos;
using PointsMall.Services;

namespace PointsMall.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FlashSalesController : ControllerBase
{
    private readonly IFlashSaleService _flashSaleService;
    private readonly IFlashSaleReservationService _reservationService;

    public FlashSalesController(IFlashSaleService flashSaleService, IFlashSaleReservationService reservationService)
    {
        _flashSaleService = flashSaleService;
        _reservationService = reservationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<FlashSaleDto>>>> GetFlashSales(
        [FromQuery] string? status = null,
        [FromQuery] int? productId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new FlashSaleQueryDto
        {
            Status = status,
            ProductId = productId,
            Page = page,
            PageSize = pageSize
        };

        var result = await _flashSaleService.GetListAsync(query);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<FlashSaleDto>>> GetFlashSale(int id)
    {
        var flashSale = await _flashSaleService.GetByIdAsync(id);
        if (flashSale == null)
        {
            return NotFound(ApiResponse.Error<FlashSaleDto>("秒杀活动不存在"));
        }
        return Ok(ApiResponse.Ok(flashSale));
    }

    [HttpGet("active/list")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<FlashSaleItemDto>>>> GetActiveFlashSales()
    {
        var result = await _flashSaleService.GetActiveFlashSalesAsync();
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id}/detail")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<FlashSaleDto>>> GetFlashSaleDetail(int id)
    {
        var flashSale = await _flashSaleService.GetFlashSaleDetailAsync(id);
        if (flashSale == null)
        {
            return NotFound(ApiResponse.Error<FlashSaleDto>("秒杀活动不存在"));
        }
        return Ok(ApiResponse.Ok(flashSale));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FlashSaleDto>>> CreateFlashSale([FromBody] CreateFlashSaleDto dto)
    {
        try
        {
            var result = await _flashSaleService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetFlashSale), new { id = result.Id }, ApiResponse.Ok(result, "秒杀活动创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Error<FlashSaleDto>(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<FlashSaleDto>>> UpdateFlashSale(int id, [FromBody] UpdateFlashSaleDto dto)
    {
        try
        {
            var result = await _flashSaleService.UpdateAsync(id, dto);
            if (result == null)
            {
                return NotFound(ApiResponse.Error<FlashSaleDto>("秒杀活动不存在"));
            }
            return Ok(ApiResponse.Ok(result, "秒杀活动更新成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Error<FlashSaleDto>(ex.Message));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteFlashSale(int id)
    {
        var result = await _flashSaleService.DeleteAsync(id);
        if (!result)
        {
            return NotFound(ApiResponse.Error("秒杀活动不存在"));
        }
        return Ok(ApiResponse.Ok("秒杀活动删除成功"));
    }

    [HttpPost("order")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateFlashSaleOrder([FromBody] CreateFlashSaleOrderDto dto)
    {
        var result = await _flashSaleService.CreateFlashSaleOrderAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return CreatedAtAction("GetOrder", "Orders", new { id = result.Data!.Id }, result);
    }

    [HttpPost("reservation")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<FlashSaleReservationDto>>> CreateReservation([FromBody] CreateFlashSaleReservationDto dto)
    {
        var result = await _reservationService.CreateReservationAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("reservation/{flashSaleId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> CancelReservation(int flashSaleId, [FromQuery] int? memberUserId)
    {
        if (!memberUserId.HasValue)
        {
            return BadRequest(ApiResponse.Error<bool>("请先登录"));
        }
        var result = await _reservationService.CancelReservationAsync(flashSaleId, memberUserId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("{flashSaleId}/reservation/status")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> GetReservationStatus(int flashSaleId, [FromQuery] int? memberUserId)
    {
        if (!memberUserId.HasValue)
        {
            return Ok(ApiResponse.Ok(false, "未登录"));
        }
        var hasReserved = await _reservationService.HasReservedAsync(flashSaleId, memberUserId.Value);
        return Ok(ApiResponse.Ok(hasReserved));
    }

    [HttpGet("reservations/my")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<FlashSaleReservationDto>>>> GetMyReservations(
        [FromQuery] int? memberUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!memberUserId.HasValue)
        {
            return BadRequest(ApiResponse.Error<PagedResult<FlashSaleReservationDto>>("请先登录"));
        }
        var query = new FlashSaleReservationQueryDto
        {
            MemberUserId = memberUserId.Value,
            Page = page,
            PageSize = pageSize
        };
        var result = await _reservationService.GetUserReservationsAsync(query);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("reminders")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<FlashSaleReminderNoticeDto>>>> GetReminderNotices(
        [FromQuery] int? memberUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!memberUserId.HasValue)
        {
            return BadRequest(ApiResponse.Error<PagedResult<FlashSaleReminderNoticeDto>>("请先登录"));
        }
        var result = await _reservationService.GetUserReminderNoticesAsync(memberUserId.Value, page, pageSize);
        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost("reminders/{noticeId}/read")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> MarkNoticeAsRead(int noticeId, [FromQuery] int? memberUserId)
    {
        if (!memberUserId.HasValue)
        {
            return BadRequest(ApiResponse.Error<bool>("请先登录"));
        }
        var result = await _reservationService.MarkNoticeAsReadAsync(noticeId, memberUserId.Value);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("reminders/unread-count")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadNoticeCount([FromQuery] int? memberUserId)
    {
        if (!memberUserId.HasValue)
        {
            return Ok(ApiResponse.Ok(0));
        }
        var count = await _reservationService.GetUnreadNoticeCountAsync(memberUserId.Value);
        return Ok(ApiResponse.Ok(count));
    }
}
