namespace PointsMall.Dtos;

public class MemberLevelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal DiscountRate { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateMemberLevelDto
{
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal DiscountRate { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateMemberLevelDto
{
    public string Name { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal DiscountRate { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
