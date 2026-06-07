namespace PointsMall.Dtos;

public class PointsRecordDto
{
    public int Id { get; set; }
    public int MemberUserId { get; set; }
    public string? MemberUsername { get; set; }
    public string? MemberNickname { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Points { get; set; }
    public int Balance { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? OrderNo { get; set; }
    public DateTime? ExpireAt { get; set; }
    public int AvailablePoints { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PointsRecordQueryDto
{
    public int? MemberUserId { get; set; }
    public string? Type { get; set; }
    public string? Search { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PointsExpirySummaryDto
{
    public int TotalPoints { get; set; }
    public int ExpiringIn7Days { get; set; }
    public int ExpiringIn30Days { get; set; }
    public DateTime? NextExpireDate { get; set; }
    public int NextExpirePoints { get; set; }
}

public class ExpiringPointsDto
{
    public int Points { get; set; }
    public DateTime ExpireDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class PointsExpiryNoticeDto
{
    public int Id { get; set; }
    public int MemberUserId { get; set; }
    public string? MemberUsername { get; set; }
    public string? MemberNickname { get; set; }
    public int PointsExpiring { get; set; }
    public DateTime ExpireDate { get; set; }
    public int DaysBeforeExpiry { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddPointsDto
{
    public int MemberUserId { get; set; }
    public int Points { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? OrderNo { get; set; }
    public DateTime? ExpireAt { get; set; }
}

public class DeductPointsDto
{
    public int MemberUserId { get; set; }
    public int Points { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? OrderNo { get; set; }
}
