using PointsMall.Dtos;

namespace PointsMall.Services;

public interface ILogisticsService
{
    Task<LogisticsTraceDto?> GetLogisticsTraceAsync(string trackingNumber, string shippingCompany);
    Task<LogisticsTraceDto?> GetLogisticsTraceByOrderIdAsync(int orderId);
    List<string> GetSupportedCompanies();
}
