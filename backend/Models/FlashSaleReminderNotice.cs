namespace PointsMall.Models;

public class FlashSaleReminderNotice
{
    public int Id { get; set; }
    public int MemberUserId { get; set; }
    public int FlashSaleId { get; set; }
    public string FlashSaleTitle { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int FlashSalePoints { get; set; }
    public DateTime StartTime { get; set; }
    public int MinutesBeforeStart { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public MemberUser? MemberUser { get; set; }
    public FlashSale? FlashSale { get; set; }
}
