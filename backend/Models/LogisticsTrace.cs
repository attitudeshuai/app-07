namespace PointsMall.Models;

public class LogisticsTrace
{
    public int Id { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string ShippingCompany { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? CurrentLocation { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime QueryTime { get; set; }
    public DateTime ExpireTime { get; set; }
    public List<LogisticsTraceItem> TraceItems { get; set; } = new();
}

public class LogisticsTraceItem
{
    public int Id { get; set; }
    public int LogisticsTraceId { get; set; }
    public DateTime Time { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Status { get; set; }
    public LogisticsTrace? LogisticsTrace { get; set; }
}
