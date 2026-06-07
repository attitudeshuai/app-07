namespace PointsMall.Models;

public class Order
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string OrderType { get; set; } = "Normal";
    public int? FlashSaleId { get; set; }
    public int? MemberUserId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int PointsConsumed { get; set; }
    public int Quantity { get; set; } = 1;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? TrackingNumber { get; set; }
    public string? ShippingCompany { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<OrderHistory> OrderHistories { get; set; } = new();
    public MemberUser? MemberUser { get; set; }
}

public class OrderHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
