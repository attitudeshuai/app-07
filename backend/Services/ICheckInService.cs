using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public interface ICheckInService
{
    Task<CheckInResultDto> CheckInAsync(int memberUserId);
    Task<CheckInStatusDto> GetCheckInStatusAsync(int memberUserId);
    Task<List<CheckInRuleDto>> GetCheckInRulesAsync();
    Task<PagedResult<CheckInRecordDto>> GetCheckInRecordsAsync(int memberUserId, int page, int pageSize);
    int GetPointsForContinuousDay(int continuousDay);
}
