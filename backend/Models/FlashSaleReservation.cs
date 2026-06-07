namespace PointsMall.Models;

public class FlashSaleReservation
{
    public int Id { get; set; }
    public int FlashSaleId { get; set; }
    public int MemberUserId { get; set; }
    public bool IsNotified { get; set; } = false;
    public DateTime? NotifiedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public FlashSale? FlashSale { get; set; }
    public MemberUser? MemberUser { get; set; }
}
