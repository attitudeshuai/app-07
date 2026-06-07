namespace PointsMall.Dtos;

public class FlashSaleDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int FlashSalePoints { get; set; }
    public int OriginalPoints { get; set; }
    public int Stock { get; set; }
    public int SoldCount { get; set; }
    public int LimitPerUser { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FlashSaleItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int FlashSalePoints { get; set; }
    public int OriginalPoints { get; set; }
    public int Stock { get; set; }
    public int SoldCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
}

public class CreateFlashSaleDto
{
    public string Title { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int FlashSalePoints { get; set; }
    public int Stock { get; set; }
    public int LimitPerUser { get; set; } = 1;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateFlashSaleDto
{
    public string Title { get; set; } = string.Empty;
    public int FlashSalePoints { get; set; }
    public int Stock { get; set; }
    public int LimitPerUser { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }
}

public class CreateFlashSaleOrderDto
{
    public int FlashSaleId { get; set; }
    public int Quantity { get; set; } = 1;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string? Remark { get; set; }
    public int? MemberUserId { get; set; }
}

public class FlashSaleQueryDto
{
    public string? Status { get; set; }
    public int? ProductId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
