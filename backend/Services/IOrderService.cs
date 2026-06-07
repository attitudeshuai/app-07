namespace PointsMall.Services;

public interface IOrderService
{
    Task<int> AutoCompleteOrdersAsync(int autoCompleteDays);
}
