namespace PointsMall.Models;

public class PointsRecord
{
    public int Id { get; set; }
    public int MemberUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public int Points { get; set; } = 0;
    public int Balance { get; set; } = 0;
    public string Source { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public string? OrderNo { get; set; }
    public DateTime? ExpireAt { get; set; }
    public int AvailablePoints { get; set; } = 0;
    public bool IsExpired { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public MemberUser? MemberUser { get; set; }
}
