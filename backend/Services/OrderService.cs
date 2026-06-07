using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Models;

namespace PointsMall.Services;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> AutoCompleteOrdersAsync(int autoCompleteDays)
    {
        var cutoffDate = DateTime.Now.AddDays(-autoCompleteDays);

        var ordersToComplete = await _context.Orders
            .Where(o => o.Status == "Shipped" &&
                        o.ShippedAt.HasValue &&
                        o.ShippedAt.Value <= cutoffDate)
            .ToListAsync();

        if (ordersToComplete.Count == 0)
        {
            return 0;
        }

        foreach (var order in ordersToComplete)
        {
            order.Status = "Completed";
            order.UpdatedAt = DateTime.Now;

            order.OrderHistories.Add(new OrderHistory
            {
                Status = "Completed",
                Remark = "系统自动确认收货"
            });
        }

        await _context.SaveChangesAsync();

        return ordersToComplete.Count;
    }
}
