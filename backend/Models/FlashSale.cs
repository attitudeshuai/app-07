namespace PointsMall.Models;

public class FlashSale
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int FlashSalePoints { get; set; }
    public int Stock { get; set; }
    public int SoldCount { get; set; } = 0;
    public int LimitPerUser { get; set; } = 1;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Product? Product { get; set; }

    public bool IsInProgress()
    {
        var now = DateTime.Now;
        return IsActive && now >= StartTime && now <= EndTime && Stock > 0;
    }

    public FlashSaleStatus GetStatus()
    {
        var now = DateTime.Now;
        if (!IsActive)
        {
            return FlashSaleStatus.Closed;
        }
        if (now < StartTime)
        {
            return FlashSaleStatus.NotStarted;
        }
        if (now > EndTime || Stock <= 0)
        {
            return FlashSaleStatus.Ended;
        }
        return FlashSaleStatus.InProgress;
    }
}

public enum FlashSaleStatus
{
    NotStarted,
    InProgress,
    Ended,
    Closed
}
