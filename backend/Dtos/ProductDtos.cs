namespace PointsMall.Dtos;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PointsRequired { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PointsRequired { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CategoryId { get; set; }
}

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PointsRequired { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int? CategoryId { get; set; }
}

public class UpdateStockDto
{
    public int Stock { get; set; }
}

public class BatchUpdateProductStatusDto
{
    public List<int> ProductIds { get; set; } = new();
    public bool IsActive { get; set; }
}

public class BatchOperationResultDto
{
    public int SuccessCount { get; set; }
    public int FailCount { get; set; }
    public List<BatchOperationErrorDto> Errors { get; set; } = new();
}

public class BatchOperationErrorDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
}
