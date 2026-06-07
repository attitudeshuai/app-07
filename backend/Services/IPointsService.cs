using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public interface IPointsService
{
    Task<PointsRecord> AddPointsAsync(AddPointsDto dto);

    Task<PointsRecord> DeductPointsAsync(DeductPointsDto dto);

    Task<int> GetAvailablePointsAsync(int memberUserId);

    Task<PointsExpirySummaryDto> GetExpirySummaryAsync(int memberUserId);

    Task<List<ExpiringPointsDto>> GetExpiringPointsAsync(int memberUserId, int days);

    Task<List<PointsRecord>> GetAvailablePointLotsAsync(int memberUserId);

    Task<int> ProcessExpiredPointsAsync();

    Task<int> GenerateExpiryNoticesAsync(int daysBeforeExpiry);

    Task<PagedResult<PointsExpiryNoticeDto>> GetExpiryNoticesAsync(int memberUserId, int page, int pageSize, bool? isRead = null);

    Task MarkNoticeAsReadAsync(int noticeId, int memberUserId);

    Task MarkAllNoticesAsReadAsync(int memberUserId);

    Task<int> GetUnreadNoticeCountAsync(int memberUserId);
}
