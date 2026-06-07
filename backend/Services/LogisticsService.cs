using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public class LogisticsService : ILogisticsService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    private const string CacheKeyPrefix = "LogisticsTrace_";
    private const int CacheDurationMinutes = 30;
    private const int DbCacheExpireHours = 6;

    private static readonly Dictionary<string, string> CompanyCodeMap = new()
    {
        { "顺丰速运", "SF" },
        { "顺丰", "SF" },
        { "圆通速递", "YTO" },
        { "圆通", "YTO" },
        { "中通快递", "ZTO" },
        { "中通", "ZTO" },
        { "韵达快递", "YD" },
        { "韵达", "YD" },
        { "申通快递", "STO" },
        { "申通", "STO" },
        { "京东物流", "JD" },
        { "京东", "JD" },
        { "百世快递", "HTKY" },
        { "百世", "HTKY" },
        { "邮政EMS", "EMS" },
        { "EMS", "EMS" },
        { "邮政包裹", "YZPY" },
        { "德邦快递", "DBL" },
        { "德邦", "DBL" }
    };

    public LogisticsService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public List<string> GetSupportedCompanies()
    {
        return CompanyCodeMap.Keys.Distinct().OrderBy(k => k).ToList();
    }

    public async Task<LogisticsTraceDto?> GetLogisticsTraceAsync(string trackingNumber, string shippingCompany)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber) || string.IsNullOrWhiteSpace(shippingCompany))
        {
            return null;
        }

        var cacheKey = $"{CacheKeyPrefix}{shippingCompany}_{trackingNumber}";

        if (_cache.TryGetValue(cacheKey, out LogisticsTraceDto? cachedTrace) && cachedTrace != null)
        {
            return cachedTrace;
        }

        var dbTrace = await _context.LogisticsTraces
            .Include(t => t.TraceItems)
            .FirstOrDefaultAsync(t => t.TrackingNumber == trackingNumber && t.ShippingCompany == shippingCompany);

        if (dbTrace != null && dbTrace.ExpireTime > DateTime.Now)
        {
            var result = MapToDto(dbTrace);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CacheDurationMinutes));
            return result;
        }

        var trace = await QueryExternalLogisticsApiAsync(trackingNumber, shippingCompany);

        if (trace != null)
        {
            await SaveOrUpdateTraceAsync(trackingNumber, shippingCompany, trace);
            _cache.Set(cacheKey, trace, TimeSpan.FromMinutes(CacheDurationMinutes));
        }

        return trace;
    }

    public async Task<LogisticsTraceDto?> GetLogisticsTraceByOrderIdAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null || string.IsNullOrEmpty(order.TrackingNumber) || string.IsNullOrEmpty(order.ShippingCompany))
        {
            return null;
        }

        return await GetLogisticsTraceAsync(order.TrackingNumber, order.ShippingCompany);
    }

    private async Task<LogisticsTraceDto?> QueryExternalLogisticsApiAsync(string trackingNumber, string shippingCompany)
    {
        var companyCode = GetCompanyCode(shippingCompany);
        if (string.IsNullOrEmpty(companyCode))
        {
            return null;
        }

        await Task.Delay(100);

        return GenerateMockTrace(trackingNumber, shippingCompany);
    }

    private LogisticsTraceDto GenerateMockTrace(string trackingNumber, string shippingCompany)
    {
        var now = DateTime.Now;
        var traceItems = new List<LogisticsTraceItemDto>();
        var random = new Random(trackingNumber.GetHashCode());

        var statuses = new[] { "揽收", "运输中", "派送中", "已签收" };
        var statusIndex = random.Next(1, 4);
        var currentStatus = statuses[statusIndex];

        var cities = new[] { "北京市", "上海市", "广州市", "深圳市", "杭州市", "成都市", "武汉市", "南京市" };
        var startCity = cities[random.Next(cities.Length)];
        var endCity = cities[random.Next(cities.Length)];

        traceItems.Add(new LogisticsTraceItemDto
        {
            Time = now.AddDays(-3).AddHours(-random.Next(12)),
            Description = $"【{startCity}】{shippingCompany} 已揽收",
            Location = startCity,
            Status = "揽收"
        });

        if (statusIndex >= 1)
        {
            var transitCities = cities
                .Where(c => c != startCity && c != endCity)
                .OrderBy(_ => random.Next())
                .Take(random.Next(1, 3))
                .ToList();

            var daysAgo = 2;
            foreach (var city in transitCities)
            {
                traceItems.Add(new LogisticsTraceItemDto
                {
                    Time = now.AddDays(-daysAgo).AddHours(-random.Next(8)),
                    Description = $"【{city}】快件已到达 {city} 转运中心",
                    Location = city,
                    Status = "运输中"
                });
                daysAgo--;
            }
        }

        if (statusIndex >= 2)
        {
            traceItems.Add(new LogisticsTraceItemDto
            {
                Time = now.AddHours(-random.Next(6, 12)),
                Description = $"【{endCity}】快件正在派送中，派送员：张师傅，电话：138****{random.Next(1000, 9999)}",
                Location = endCity,
                Status = "派送中"
            });
        }

        if (statusIndex >= 3)
        {
            traceItems.Add(new LogisticsTraceItemDto
            {
                Time = now.AddHours(-random.Next(1, 5)),
                Description = $"【{endCity}】快件已签收，签收人：本人签收",
                Location = endCity,
                Status = "已签收"
            });
        }

        traceItems = traceItems.OrderByDescending(t => t.Time).ToList();

        var estimatedDelivery = currentStatus == "已签收"
            ? (DateTime?)null
            : now.AddDays(random.Next(1, 4)).Date.AddHours(random.Next(9, 18));

        return new LogisticsTraceDto
        {
            TrackingNumber = trackingNumber,
            ShippingCompany = shippingCompany,
            Status = currentStatus,
            CurrentLocation = traceItems.FirstOrDefault()?.Location,
            EstimatedDelivery = estimatedDelivery,
            QueryTime = now,
            TraceItems = traceItems
        };
    }

    private async Task SaveOrUpdateTraceAsync(string trackingNumber, string shippingCompany, LogisticsTraceDto traceDto)
    {
        var existingTrace = await _context.LogisticsTraces
            .Include(t => t.TraceItems)
            .FirstOrDefaultAsync(t => t.TrackingNumber == trackingNumber && t.ShippingCompany == shippingCompany);

        if (existingTrace != null)
        {
            existingTrace.Status = traceDto.Status;
            existingTrace.CurrentLocation = traceDto.CurrentLocation;
            existingTrace.EstimatedDelivery = traceDto.EstimatedDelivery;
            existingTrace.QueryTime = traceDto.QueryTime;
            existingTrace.ExpireTime = DateTime.Now.AddHours(DbCacheExpireHours);

            _context.LogisticsTraceItems.RemoveRange(existingTrace.TraceItems);

            foreach (var item in traceDto.TraceItems)
            {
                existingTrace.TraceItems.Add(new LogisticsTraceItem
                {
                    Time = item.Time,
                    Description = item.Description,
                    Location = item.Location,
                    Status = item.Status
                });
            }
        }
        else
        {
            var trace = new LogisticsTrace
            {
                TrackingNumber = trackingNumber,
                ShippingCompany = shippingCompany,
                Status = traceDto.Status,
                CurrentLocation = traceDto.CurrentLocation,
                EstimatedDelivery = traceDto.EstimatedDelivery,
                QueryTime = traceDto.QueryTime,
                ExpireTime = DateTime.Now.AddHours(DbCacheExpireHours),
                TraceItems = traceDto.TraceItems.Select(item => new LogisticsTraceItem
                {
                    Time = item.Time,
                    Description = item.Description,
                    Location = item.Location,
                    Status = item.Status
                }).ToList()
            };

            _context.LogisticsTraces.Add(trace);
        }

        await _context.SaveChangesAsync();
    }

    private static string GetCompanyCode(string companyName)
    {
        if (CompanyCodeMap.TryGetValue(companyName, out var code))
        {
            return code;
        }
        return string.Empty;
    }

    private static LogisticsTraceDto MapToDto(LogisticsTrace trace)
    {
        return new LogisticsTraceDto
        {
            TrackingNumber = trace.TrackingNumber,
            ShippingCompany = trace.ShippingCompany,
            Status = trace.Status,
            CurrentLocation = trace.CurrentLocation,
            EstimatedDelivery = trace.EstimatedDelivery,
            QueryTime = trace.QueryTime,
            TraceItems = trace.TraceItems
                .OrderByDescending(i => i.Time)
                .Select(i => new LogisticsTraceItemDto
                {
                    Time = i.Time,
                    Description = i.Description,
                    Location = i.Location,
                    Status = i.Status
                })
                .ToList()
        };
    }
}
