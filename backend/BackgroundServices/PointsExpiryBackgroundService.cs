using PointsMall.Services;

namespace PointsMall.BackgroundServices;

public class PointsExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PointsExpiryBackgroundService> _logger;

    public PointsExpiryBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PointsExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Points Expiry Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var checkIntervalHours = _configuration.GetValue<int>("PointsExpiry:CheckIntervalHours", 24);
                var noticeDays = _configuration.GetSection("PointsExpiry:NoticeDays").Get<int[]>()
                    ?? new[] { 7, 3, 1 };

                using (var scope = _serviceProvider.CreateScope())
                {
                    var pointsService = scope.ServiceProvider.GetRequiredService<IPointsService>();

                    var expiredCount = await pointsService.ProcessExpiredPointsAsync();
                    if (expiredCount > 0)
                    {
                        _logger.LogInformation("Processed {Count} expired point lots.", expiredCount);
                    }

                    foreach (var days in noticeDays)
                    {
                        var noticeCount = await pointsService.GenerateExpiryNoticesAsync(days);
                        if (noticeCount > 0)
                        {
                            _logger.LogInformation(
                                "Generated {Count} expiry notices for {Days} days before expiry.",
                                noticeCount, days);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromHours(checkIntervalHours), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing points expiry.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Points Expiry Background Service is stopping.");
    }
}
