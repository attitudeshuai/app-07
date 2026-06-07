namespace PointsMall.Models;

public class FlashSaleUserPurchase
{
    public int Id { get; set; }
    public int FlashSaleId { get; set; }
    public int MemberUserId { get; set; }
    public int Quantity { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
