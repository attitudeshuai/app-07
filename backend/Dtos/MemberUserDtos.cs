namespace PointsMall.Dtos;

public class MemberUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public int Points { get; set; }
    public int TotalPoints { get; set; }
    public int ContinuousCheckInDays { get; set; }
    public int TotalCheckInDays { get; set; }
    public DateTime? LastCheckInDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int OrderCount { get; set; }
    public int PointsRecordCount { get; set; }
    public int CheckInRecordCount { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public decimal DiscountRate { get; set; } = 1.0m;
}

public class CreateMemberUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Status { get; set; } = "Active";
}

public class UpdateMemberUserDto
{
    public string Nickname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class AdjustPointsDto
{
    public int Points { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
}

public class MemberUserQueryDto
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
