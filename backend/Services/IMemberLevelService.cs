using PointsMall.Models;

namespace PointsMall.Services;

public interface IMemberLevelService
{
    Task<MemberLevel?> GetLevelByTotalPointsAsync(int totalPoints);
    Task<List<MemberLevel>> GetActiveLevelsAsync();
    Task<decimal> GetDiscountRateAsync(int totalPoints);
    Task<int> CalculateDiscountedPointsAsync(int basePoints, int totalPoints);
}
