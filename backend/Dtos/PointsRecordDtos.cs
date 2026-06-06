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
