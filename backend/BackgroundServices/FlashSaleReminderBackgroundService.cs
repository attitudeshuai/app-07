using PointsMall.Services;

namespace PointsMall.BackgroundServices;

public class FlashSaleReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FlashSaleReminderBackgroundService> _logger;

    public FlashSaleReminderBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<FlashSaleReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Flash Sale Reminder Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var checkIntervalMinutes = _configuration.GetValue<int>("FlashSaleReminder:CheckIntervalMinutes", 1);
                var reminderMinutesBefore = _configuration.GetSection("FlashSaleReminder:ReminderMinutesBefore").Get<int[]>()
                    ?? new[] { 30, 10, 5 };

                using (var scope = _serviceProvider.CreateScope())
                {
                    var reservationService = scope.ServiceProvider.GetRequiredService<IFlashSaleReservationService>();

                    var now = DateTime.Now;

                    foreach (var minutes in reminderMinutesBefore)
                    {
                        var targetTime = now.AddMinutes(minutes);
                        var windowStart = targetTime.AddMinutes(-checkIntervalMinutes);
                        var windowEnd = targetTime;

                        var reservations = await reservationService.GetReservationsForReminderAsync(windowStart, windowEnd);

                        if (reservations.Count > 0)
                        {
                            var flashSaleGroups = reservations.GroupBy(r => r.FlashSaleId);

                            foreach (var group in flashSaleGroups)
                            {
                                var result = await reservationService.CreateReminderNoticesAsync(group.Key, minutes);
                                if (result.Success && result.Data > 0)
                                {
                                    _logger.LogInformation(
                                        "Created {Count} reminder notices for flash sale {FlashSaleId}, {Minutes} minutes before start.",
                                        result.Data, group.Key, minutes);
                                }
                            }
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(checkIntervalMinutes), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing flash sale reminders.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Flash Sale Reminder Background Service is stopping.");
    }
}
