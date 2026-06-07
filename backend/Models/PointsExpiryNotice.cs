namespace PointsMall.Models;

public class PointsExpiryNotice
{
    public int Id { get; set; }
    public int MemberUserId { get; set; }
    public int PointsExpiring { get; set; }
    public DateTime ExpireDate { get; set; }
    public int DaysBeforeExpiry { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public MemberUser? MemberUser { get; set; }
}
