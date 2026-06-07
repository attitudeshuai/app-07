namespace PointsMall.Dtos;

public class LogisticsTraceDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string ShippingCompany { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? CurrentLocation { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime QueryTime { get; set; }
    public List<LogisticsTraceItemDto> TraceItems { get; set; } = new();
}

public class LogisticsTraceItemDto
{
    public DateTime Time { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Status { get; set; }
}
