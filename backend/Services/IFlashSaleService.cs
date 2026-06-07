using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Services;

public interface IFlashSaleService
{
    Task<FlashSaleDto?> GetByIdAsync(int id);
    Task<PagedResult<FlashSaleDto>> GetListAsync(FlashSaleQueryDto query);
    Task<List<FlashSaleItemDto>> GetActiveFlashSalesAsync();
    Task<FlashSaleDto?> GetFlashSaleDetailAsync(int id);
    Task<FlashSaleDto> CreateAsync(CreateFlashSaleDto dto);
    Task<FlashSaleDto?> UpdateAsync(int id, UpdateFlashSaleDto dto);
    Task<bool> DeleteAsync(int id);
    Task<ApiResponse<OrderDto>> CreateFlashSaleOrderAsync(CreateFlashSaleOrderDto dto);
    Task ReturnStockAsync(int flashSaleId, int quantity);
}
