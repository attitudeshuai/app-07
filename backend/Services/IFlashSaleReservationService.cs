using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public interface IFlashSaleReservationService
{
    Task<ApiResponse<FlashSaleReservationDto>> CreateReservationAsync(CreateFlashSaleReservationDto dto);
    Task<ApiResponse<bool>> CancelReservationAsync(int flashSaleId, int memberUserId);
    Task<bool> HasReservedAsync(int flashSaleId, int memberUserId);
    Task<PagedResult<FlashSaleReservationDto>> GetUserReservationsAsync(FlashSaleReservationQueryDto query);
    Task<int> GetReservationCountAsync(int flashSaleId);
    Task<List<FlashSaleReservation>> GetReservationsForReminderAsync(DateTime startTimeFrom, DateTime startTimeTo);
    Task MarkAsNotifiedAsync(int reservationId);
    Task<ApiResponse<int>> CreateReminderNoticesAsync(int flashSaleId, int minutesBeforeStart);
    Task<PagedResult<FlashSaleReminderNoticeDto>> GetUserReminderNoticesAsync(int memberUserId, int page, int pageSize);
    Task<ApiResponse<bool>> MarkNoticeAsReadAsync(int noticeId, int memberUserId);
    Task<int> GetUnreadNoticeCountAsync(int memberUserId);
}
