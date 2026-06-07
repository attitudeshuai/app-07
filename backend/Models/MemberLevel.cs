namespace PointsMall.Models;

public class MemberLevel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; } = 0;
    public decimal DiscountRate { get; set; } = 1.0m;
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
