namespace PointsMall.Dtos;

public class CheckInRecordDto
{
    public int Id { get; set; }
    public int MemberUserId { get; set; }
    public string? MemberUsername { get; set; }
    public string? MemberNickname { get; set; }
    public DateTime CheckInDate { get; set; }
    public int PointsEarned { get; set; }
    public int ContinuousDays { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CheckInResultDto
{
    public bool Success { get; set; }
    public int PointsEarned { get; set; }
    public int ContinuousDays { get; set; }
    public int TotalPoints { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CheckInStatusDto
{
    public bool CanCheckIn { get; set; }
    public int ContinuousDays { get; set; }
    public int TotalCheckInDays { get; set; }
    public DateTime? LastCheckInDate { get; set; }
    public int TodayPoints { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CheckInRuleDto
{
    public int Day { get; set; }
    public int Points { get; set; }
}

public class DoCheckInDto
{
    public int MemberUserId { get; set; }
}
