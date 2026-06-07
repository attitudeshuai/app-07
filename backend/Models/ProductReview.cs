namespace PointsMall.Models;

public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int MemberUserId { get; set; }
    public MemberUser? MemberUser { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
