using PointsMall.Services;

namespace PointsMall.BackgroundServices;

public class OrderAutoCompleteService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderAutoCompleteService> _logger;

    public OrderAutoCompleteService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OrderAutoCompleteService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Auto-Complete Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var autoCompleteDays = _configuration.GetValue<int>("OrderAutoComplete:Days", 7);
                var checkIntervalMinutes = _configuration.GetValue<int>("OrderAutoComplete:CheckIntervalMinutes", 60);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                    var completedCount = await orderService.AutoCompleteOrdersAsync(autoCompleteDays);

                    if (completedCount > 0)
                    {
                        _logger.LogInformation("Auto-completed {Count} orders after {Days} days.", completedCount, autoCompleteDays);
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
                _logger.LogError(ex, "Error occurred while auto-completing orders.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Order Auto-Complete Service is stopping.");
    }
}
