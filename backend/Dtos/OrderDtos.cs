namespace PointsMall.Dtos;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public int? FlashSaleId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int PointsConsumed { get; set; }
    public int Quantity { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? ShippingCompany { get; set; }
    public string? Remark { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderHistoryDto> OrderHistories { get; set; } = new();
    public LogisticsTraceDto? LogisticsTrace { get; set; }
    public bool HasReview { get; set; }
    public int? AutoCompleteDaysLeft { get; set; }
    public List<OrderPackageDto> Packages { get; set; } = new();
}

public class OrderHistoryDto
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOrderDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public int? MemberUserId { get; set; }
}

public class ShipOrderDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string ShippingCompany { get; set; } = string.Empty;
    public string? Remark { get; set; }
}

public class ReturnOrderDto
{
    public string Reason { get; set; } = string.Empty;
}

public class OrderQueryDto
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}

public class OrderPackageDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string PackageNo { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? ShippingCompany { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderPackageItemDto> Items { get; set; } = new();
    public LogisticsTraceDto? LogisticsTrace { get; set; }
}

public class OrderPackageItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class CreateOrderPackageDto
{
    public int OrderId { get; set; }
    public string? Remark { get; set; }
    public List<CreateOrderPackageItemDto> Items { get; set; } = new();
}

public class CreateOrderPackageItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateOrderPackageDto
{
    public string? TrackingNumber { get; set; }
    public string? ShippingCompany { get; set; }
    public string? Remark { get; set; }
}

public class ShipPackageDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public string ShippingCompany { get; set; } = string.Empty;
    public string? Remark { get; set; }
}
