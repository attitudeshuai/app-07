namespace PointsMall.Models;

public class OrderPackage
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string PackageNo { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? ShippingCompany { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Remark { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public List<OrderPackageItem> Items { get; set; } = new();
    public Order? Order { get; set; }
}

public class OrderPackageItem
{
    public int Id { get; set; }
    public int OrderPackageId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public OrderPackage? OrderPackage { get; set; }
}
