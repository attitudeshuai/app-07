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
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (categoryId.HasValue)
        {
            var childCategoryIds = await _context.Categories
                .Where(c => c.ParentId == categoryId.Value)
                .Select(c => c.Id)
                .ToListAsync();

            var allCategoryIds = new List<int> { categoryId.Value };
            allCategoryIds.AddRange(childCategoryIds);

            query = query.Where(p => p.CategoryId.HasValue && allCategoryIds.Contains(p.CategoryId.Value));
        }

        var total = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                PointsRequired = p.PointsRequired,
                Stock = p.Stock,
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        var result = new PagedResult<ProductDto>
        {
            Items = products,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse.Ok(result));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(ApiResponse.Error<ProductDto>("商品不存在"));
        }

        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            PointsRequired = product.PointsRequired,
            Stock = product.Stock,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = product.Category != null ? product.Category.Name : null,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto dto)
    {
        if (dto.CategoryId.HasValue)
        {
            var category = await _context.Categories.FindAsync(dto.CategoryId.Value);
            if (category == null)
            {
                return BadRequest(ApiResponse.Error<ProductDto>("所选分类不存在"));
            }
        }

        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            PointsRequired = dto.PointsRequired,
            Stock = dto.Stock,
            ImageUrl = dto.ImageUrl,
            IsActive = dto.IsActive,
            CategoryId = dto.CategoryId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            PointsRequired = product.PointsRequired,
            Stock = product.Stock,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = product.Category != null ? product.Category.Name : null,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ApiResponse.Ok(result, "商品创建成功"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(ApiResponse.Error<ProductDto>("商品不存在"));
        }

        if (dto.CategoryId.HasValue)
        {
            var category = await _context.Categories.FindAsync(dto.CategoryId.Value);
            if (category == null)
            {
                return BadRequest(ApiResponse.Error<ProductDto>("所选分类不存在"));
            }
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.PointsRequired = dto.PointsRequired;
        product.Stock = dto.Stock;
        product.ImageUrl = dto.ImageUrl;
        product.IsActive = dto.IsActive;
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var result = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            PointsRequired = product.PointsRequired,
            Stock = product.Stock,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId,
            CategoryName = product.Category != null ? product.Category.Name : null,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return Ok(ApiResponse.Ok(result, "商品更新成功"));
    }

    [HttpPut("{id}/stock")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(ApiResponse.Error("商品不存在"));
        }

        product.Stock = dto.Stock;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("库存更新成功"));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(ApiResponse.Error("商品不存在"));
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("商品删除成功"));
    }
}
