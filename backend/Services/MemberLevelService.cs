using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Models;

namespace PointsMall.Services;

public class MemberLevelService : IMemberLevelService
{
    private readonly ApplicationDbContext _context;

    public MemberLevelService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MemberLevel?> GetLevelByTotalPointsAsync(int totalPoints)
    {
        return await _context.MemberLevels
            .Where(l => l.IsActive && l.MinPoints <= totalPoints)
            .OrderByDescending(l => l.MinPoints)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MemberLevel>> GetActiveLevelsAsync()
    {
        return await _context.MemberLevels
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.MinPoints)
            .ToListAsync();
    }

    public async Task<decimal> GetDiscountRateAsync(int totalPoints)
    {
        var level = await GetLevelByTotalPointsAsync(totalPoints);
        return level?.DiscountRate ?? 1.0m;
    }

    public async Task<int> CalculateDiscountedPointsAsync(int basePoints, int totalPoints)
    {
        var discountRate = await GetDiscountRateAsync(totalPoints);
        var discountedPoints = basePoints * discountRate;
        return (int)Math.Ceiling(discountedPoints);
    }
}
