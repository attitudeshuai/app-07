using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PointsMall.Data;
using PointsMall.Dtos;
using PointsMall.Models;

namespace PointsMall.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("product/{productId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductReviewDto>>>> GetProductReviews(
        int productId,
        [FromQuery] int? rating = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortOrder = "Desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(ApiResponse.Error<PagedResult<ProductReviewDto>>("商品不存在"));
        }

        var isAdmin = IsCurrentUserAdmin();

        var query = _context.ProductReviews
            .Include(r => r.MemberUser)
            .Where(r => r.ProductId == productId)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(r => !r.IsHidden);
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        query = sortBy.ToLower() switch
        {
            "rating" => sortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(r => r.Rating)
                : query.OrderByDescending(r => r.Rating),
            "createdat" or _ => sortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt)
        };

        var total = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                OrderId = r.OrderId,
                MemberUserId = r.MemberUserId,
                MemberUserNickname = r.MemberUser != null ? r.MemberUser.Nickname : null,
                MemberUserAvatar = r.MemberUser != null ? r.MemberUser.Avatar : null,
                Rating = r.Rating,
                Content = r.Content,
                IsHidden = r.IsHidden,
                MerchantReply = r.MerchantReply,
                MerchantReplyAt = r.MerchantReplyAt,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        var result = new PagedResult<ProductReviewDto>
        {
            Items = reviews,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("product/{productId}/stats")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ProductReviewStatsDto>>> GetProductReviewStats(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound(ApiResponse.Error<ProductReviewStatsDto>("商品不存在"));
        }

        var isAdmin = IsCurrentUserAdmin();

        var query = _context.ProductReviews
            .Where(r => r.ProductId == productId);

        if (!isAdmin)
        {
            query = query.Where(r => !r.IsHidden);
        }

        var reviews = await query.ToListAsync();

        var stats = new ProductReviewStatsDto
        {
            ProductId = productId,
            TotalReviews = reviews.Count,
            AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 2) : 0,
            Rating1Count = reviews.Count(r => r.Rating == 1),
            Rating2Count = reviews.Count(r => r.Rating == 2),
            Rating3Count = reviews.Count(r => r.Rating == 3),
            Rating4Count = reviews.Count(r => r.Rating == 4),
            Rating5Count = reviews.Count(r => r.Rating == 5)
        };

        return Ok(ApiResponse.Ok(stats));
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductReviewDto>>>> GetAllReviews(
        [FromQuery] bool? isHidden = null,
        [FromQuery] int? rating = null,
        [FromQuery] int? productId = null,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortOrder = "Desc",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.ProductReviews
            .Include(r => r.MemberUser)
            .Include(r => r.Product)
            .AsQueryable();

        if (isHidden.HasValue)
        {
            query = query.Where(r => r.IsHidden == isHidden.Value);
        }

        if (rating.HasValue)
        {
            query = query.Where(r => r.Rating == rating.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(r => r.ProductId == productId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => r.Content != null && r.Content.Contains(search) ||
                                     r.MemberUser != null && r.MemberUser.Nickname != null && r.MemberUser.Nickname.Contains(search) ||
                                     r.Product != null && r.Product.Name.Contains(search));
        }

        query = sortBy.ToLower() switch
        {
            "rating" => sortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(r => r.Rating)
                : query.OrderByDescending(r => r.Rating),
            "createdat" or _ => sortOrder.Equals("Asc", StringComparison.OrdinalIgnoreCase)
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt)
        };

        var total = await query.CountAsync();
        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ProductReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                OrderId = r.OrderId,
                MemberUserId = r.MemberUserId,
                MemberUserNickname = r.MemberUser != null ? r.MemberUser.Nickname : null,
                MemberUserAvatar = r.MemberUser != null ? r.MemberUser.Avatar : null,
                Rating = r.Rating,
                Content = r.Content,
                IsHidden = r.IsHidden,
                MerchantReply = r.MerchantReply,
                MerchantReplyAt = r.MerchantReplyAt,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();

        var result = new PagedResult<ProductReviewDto>
        {
            Items = reviews,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductReviewDto>>> CreateReview([FromBody] CreateProductReviewDto dto)
    {
        var memberUserId = GetCurrentMemberUserId();
        if (!memberUserId.HasValue)
        {
            return Unauthorized(ApiResponse.Error<ProductReviewDto>("身份验证失败"));
        }

        if (dto.Rating < 1 || dto.Rating > 5)
        {
            return BadRequest(ApiResponse.Error<ProductReviewDto>("评分必须在1到5之间"));
        }

        var order = await _context.Orders.FindAsync(dto.OrderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<ProductReviewDto>("订单不存在"));
        }

        if (order.MemberUserId != memberUserId.Value)
        {
            return Unauthorized(ApiResponse.Error<ProductReviewDto>("无权对该订单进行评价"));
        }

        if (order.Status != "Completed")
        {
            return BadRequest(ApiResponse.Error<ProductReviewDto>("订单未完成，无法评价"));
        }

        var existingReview = await _context.ProductReviews
            .FirstOrDefaultAsync(r => r.OrderId == dto.OrderId);
        if (existingReview != null)
        {
            return BadRequest(ApiResponse.Error<ProductReviewDto>("该订单已评价过了"));
        }

        var review = new ProductReview
        {
            ProductId = order.ProductId,
            OrderId = dto.OrderId,
            MemberUserId = memberUserId.Value,
            Rating = dto.Rating,
            Content = dto.Content,
            IsHidden = false
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        var memberUser = await _context.MemberUsers.FindAsync(memberUserId.Value);
        var result = new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            OrderId = review.OrderId,
            MemberUserId = review.MemberUserId,
            MemberUserNickname = memberUser?.Nickname,
            MemberUserAvatar = memberUser?.Avatar,
            Rating = review.Rating,
            Content = review.Content,
            IsHidden = review.IsHidden,
            MerchantReply = review.MerchantReply,
            MerchantReplyAt = review.MerchantReplyAt,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };

        return CreatedAtAction(nameof(GetProductReviews), new { productId = review.ProductId }, ApiResponse.Ok(result, "评价发布成功"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductReviewDto>>> GetReview(int id)
    {
        var review = await _context.ProductReviews
            .Include(r => r.MemberUser)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            return NotFound(ApiResponse.Error<ProductReviewDto>("评价不存在"));
        }

        var memberUserId = GetCurrentMemberUserId();
        var isAdmin = IsCurrentUserAdmin();

        if (!isAdmin && (!memberUserId.HasValue || review.MemberUserId != memberUserId.Value))
        {
            if (review.IsHidden)
            {
                return NotFound(ApiResponse.Error<ProductReviewDto>("评价不存在"));
            }
        }

        var dto = new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            OrderId = review.OrderId,
            MemberUserId = review.MemberUserId,
            MemberUserNickname = review.MemberUser != null ? review.MemberUser.Nickname : null,
            MemberUserAvatar = review.MemberUser != null ? review.MemberUser.Avatar : null,
            Rating = review.Rating,
            Content = review.Content,
            IsHidden = review.IsHidden,
            MerchantReply = review.MerchantReply,
            MerchantReplyAt = review.MerchantReplyAt,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<ApiResponse<ProductReviewDto>>> GetOrderReview(int orderId)
    {
        var memberUserId = GetCurrentMemberUserId();
        if (!memberUserId.HasValue)
        {
            return Unauthorized(ApiResponse.Error<ProductReviewDto>("身份验证失败"));
        }

        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
        {
            return NotFound(ApiResponse.Error<ProductReviewDto>("订单不存在"));
        }

        if (order.MemberUserId != memberUserId.Value && !IsCurrentUserAdmin())
        {
            return Unauthorized(ApiResponse.Error<ProductReviewDto>("无权查看该评价"));
        }

        var review = await _context.ProductReviews
            .Include(r => r.MemberUser)
            .FirstOrDefaultAsync(r => r.OrderId == orderId);

        if (review == null)
        {
            return NotFound(ApiResponse.Error<ProductReviewDto>("该订单暂无评价"));
        }

        var dto = new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            OrderId = review.OrderId,
            MemberUserId = review.MemberUserId,
            MemberUserNickname = review.MemberUser != null ? review.MemberUser.Nickname : null,
            MemberUserAvatar = review.MemberUser != null ? review.MemberUser.Avatar : null,
            Rating = review.Rating,
            Content = review.Content,
            IsHidden = review.IsHidden,
            MerchantReply = review.MerchantReply,
            MerchantReplyAt = review.MerchantReplyAt,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpPut("{id}/hide")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> HideReview(int id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null)
        {
            return NotFound(ApiResponse.Error("评价不存在"));
        }

        review.IsHidden = true;
        review.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("评价已隐藏"));
    }

    [HttpPut("{id}/show")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ShowReview(int id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null)
        {
            return NotFound(ApiResponse.Error("评价不存在"));
        }

        review.IsHidden = false;
        review.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("评价已显示"));
    }

    [HttpPost("{id}/reply")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<ProductReviewDto>>> ReplyReview(int id, [FromBody] MerchantReplyDto dto)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null)
        {
            return NotFound(ApiResponse.Error<ProductReviewDto>("评价不存在"));
        }

        if (string.IsNullOrWhiteSpace(dto.ReplyContent))
        {
            return BadRequest(ApiResponse.Error<ProductReviewDto>("回复内容不能为空"));
        }

        review.MerchantReply = dto.ReplyContent;
        review.MerchantReplyAt = DateTime.Now;
        review.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var reviewDto = new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            OrderId = review.OrderId,
            MemberUserId = review.MemberUserId,
            Rating = review.Rating,
            Content = review.Content,
            IsHidden = review.IsHidden,
            MerchantReply = review.MerchantReply,
            MerchantReplyAt = review.MerchantReplyAt,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };

        return Ok(ApiResponse.Ok(reviewDto, "回复成功"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReview(int id)
    {
        var review = await _context.ProductReviews.FindAsync(id);
        if (review == null)
        {
            return NotFound(ApiResponse.Error("评价不存在"));
        }

        var memberUserId = GetCurrentMemberUserId();
        var isAdmin = IsCurrentUserAdmin();

        if (!isAdmin && (!memberUserId.HasValue || review.MemberUserId != memberUserId.Value))
        {
            return Unauthorized(ApiResponse.Error("无权删除该评价"));
        }

        _context.ProductReviews.Remove(review);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("评价删除成功"));
    }

    private int? GetCurrentMemberUserId()
    {
        var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (nameIdClaim != null && int.TryParse(nameIdClaim.Value, out var userId))
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            if (roleClaim != null && roleClaim.Value == "Member")
            {
                return userId;
            }
        }
        return null;
    }

    private bool IsCurrentUserAdmin()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        return roleClaim != null && roleClaim.Value == "Admin";
    }
}
