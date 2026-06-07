namespace PointsMall.Models;

public class CheckInRecord
{
    public int Id { get; set; }
    public int MemberUserId { get; set; }
    public DateTime CheckInDate { get; set; }
    public int PointsEarned { get; set; }
    public int ContinuousDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public MemberUser? MemberUser { get; set; }
}
