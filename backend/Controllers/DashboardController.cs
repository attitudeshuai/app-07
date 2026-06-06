using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Dtos;

namespace PointsMall.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats()
    {
        var totalProducts = await _context.Products.CountAsync();
        var totalOrders = await _context.Orders.CountAsync();
        var totalMembers = await _context.MemberUsers.CountAsync();
        var totalPoints = await _context.MemberUsers.SumAsync(u => u.Points);

        var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
        var shippedOrders = await _context.Orders.CountAsync(o => o.Status == "Shipped");
        var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");

        var lowStockProducts = await _context.Products
            .Where(p => p.Stock < 10 && p.IsActive)
            .CountAsync();

        var recentOrders = await _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new OrderDto
            {
                Id = o.Id,
                OrderNo = o.OrderNo,
                ProductId = o.ProductId,
                ProductName = o.ProductName,
                PointsConsumed = o.PointsConsumed,
                Quantity = o.Quantity,
                RecipientName = o.RecipientName,
                RecipientPhone = o.RecipientPhone,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        var stats = new DashboardStatsDto
        {
            TotalProducts = totalProducts,
            TotalOrders = totalOrders,
            TotalMembers = totalMembers,
            TotalPoints = totalPoints,
            PendingOrders = pendingOrders,
            ShippedOrders = shippedOrders,
            CompletedOrders = completedOrders,
            LowStockProducts = lowStockProducts,
            RecentOrders = recentOrders
        };

        return Ok(ApiResponse.Ok(stats));
    }

    [HttpGet("order-trend")]
    public async Task<ActionResult<ApiResponse<List<OrderTrendDto>>>> GetOrderTrend([FromQuery] int days = 7)
    {
        var startDate = DateTime.Now.Date.AddDays(-days + 1);

        var ordersByDate = await _context.Orders
            .Where(o => o.CreatedAt >= startDate)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                TotalPoints = g.Sum(o => o.PointsConsumed)
            })
            .ToListAsync();

        var result = new List<OrderTrendDto>();
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            var dayData = ordersByDate.FirstOrDefault(d => d.Date == date);
            result.Add(new OrderTrendDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                OrderCount = dayData?.Count ?? 0,
                PointsConsumed = dayData?.TotalPoints ?? 0
            });
        }

        return Ok(ApiResponse.Ok(result));
    }
}

public class DashboardStatsDto
{
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalMembers { get; set; }
    public int TotalPoints { get; set; }
    public int PendingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int LowStockProducts { get; set; }
    public List<OrderDto> RecentOrders { get; set; } = new();
}

public class OrderTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int PointsConsumed { get; set; }
}
