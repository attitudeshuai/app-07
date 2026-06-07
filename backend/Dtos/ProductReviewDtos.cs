namespace PointsMall.Dtos;

public class ProductReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int OrderId { get; set; }
    public int MemberUserId { get; set; }
    public string? MemberUserNickname { get; set; }
    public string? MemberUserAvatar { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
    public bool IsHidden { get; set; }
    public string? MerchantReply { get; set; }
    public DateTime? MerchantReplyAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProductReviewDto
{
    public int OrderId { get; set; }
    public int Rating { get; set; }
    public string? Content { get; set; }
}

public class MerchantReplyDto
{
    public string ReplyContent { get; set; } = string.Empty;
}

public class ProductReviewStatsDto
{
    public int ProductId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int Rating1Count { get; set; }
    public int Rating2Count { get; set; }
    public int Rating3Count { get; set; }
    public int Rating4Count { get; set; }
    public int Rating5Count { get; set; }
}

public class ProductReviewQueryDto
{
    public int? Rating { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "Desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
