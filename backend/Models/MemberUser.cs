namespace PointsMall.Models;

public class MemberUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public int Points { get; set; } = 0;
    public int TotalPoints { get; set; } = 0;
    public int ContinuousCheckInDays { get; set; } = 0;
    public int TotalCheckInDays { get; set; } = 0;
    public DateTime? LastCheckInDate { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<PointsRecord> PointsRecords { get; set; } = new List<PointsRecord>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CheckInRecord> CheckInRecords { get; set; } = new List<CheckInRecord>();
}
