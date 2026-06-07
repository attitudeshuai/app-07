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
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("tree")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategoryTree()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync();

        var categoryDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentId = c.ParentId,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        var tree = BuildTree(categoryDtos, null);

        return Ok(ApiResponse.Ok(tree));
    }

    [HttpGet("active/tree")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetActiveCategoryTree()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync();

        var categoryDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentId = c.ParentId,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        var tree = BuildTree(categoryDtos, null);

        return Ok(ApiResponse.Ok(tree));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategorySimpleDto>>>> GetCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .ToListAsync();

        var dtos = categories.Select(c => new CategorySimpleDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentId = c.ParentId
        }).ToList();

        return Ok(ApiResponse.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(ApiResponse.Error<CategoryDto>("分类不存在"));
        }

        var dto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return Ok(ApiResponse.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(ApiResponse.Error<CategoryDto>("分类名称不能为空"));
        }

        if (dto.ParentId.HasValue)
        {
            var parent = await _context.Categories.FindAsync(dto.ParentId.Value);
            if (parent == null)
            {
                return BadRequest(ApiResponse.Error<CategoryDto>("父分类不存在"));
            }

            if (parent.ParentId.HasValue)
            {
                return BadRequest(ApiResponse.Error<CategoryDto>("最多支持两级分类，不能在三级分类下创建子分类"));
            }
        }

        var category = new Category
        {
            Name = dto.Name,
            ParentId = dto.ParentId,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, ApiResponse.Ok(result, "分类创建成功"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(ApiResponse.Error<object>("分类不存在"));
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(ApiResponse.Error<object>("分类名称不能为空"));
        }

        if (dto.ParentId.HasValue && dto.ParentId.Value == id)
        {
            return BadRequest(ApiResponse.Error<object>("不能将自己设为父分类"));
        }

        if (dto.ParentId.HasValue)
        {
            var parent = await _context.Categories.FindAsync(dto.ParentId.Value);
            if (parent == null)
            {
                return BadRequest(ApiResponse.Error<object>("父分类不存在"));
            }

            if (parent.ParentId.HasValue)
            {
                return BadRequest(ApiResponse.Error<object>("最多支持两级分类，不能在三级分类下创建子分类"));
            }

            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
            if (hasChildren && dto.ParentId.HasValue)
            {
                return BadRequest(ApiResponse.Error<object>("当前分类有子分类，不能降级为二级分类"));
            }
        }

        category.Name = dto.Name;
        category.ParentId = dto.ParentId;
        category.SortOrder = dto.SortOrder;
        category.IsActive = dto.IsActive;
        category.UpdatedAt = DateTime.Now;

        _context.Entry(category).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CategoryExists(id))
            {
                return NotFound(ApiResponse.Error<object>("分类不存在"));
            }
            else
            {
                throw;
            }
        }

        return Ok(ApiResponse.Ok<object>(new { message = "更新成功" }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound(ApiResponse.Error<object>("分类不存在"));
        }

        var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
        if (hasChildren)
        {
            return BadRequest(ApiResponse.Error<object>("该分类下存在子分类，请先删除子分类"));
        }

        var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
        {
            return BadRequest(ApiResponse.Error<object>("该分类下存在商品，请先将商品移到其他分类"));
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("分类删除成功"));
    }

    private static List<CategoryDto> BuildTree(List<CategoryDto> categories, int? parentId)
    {
        return categories
            .Where(c => c.ParentId == parentId)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Children = BuildTree(categories, c.Id)
            })
            .ToList();
    }

    private bool CategoryExists(int id)
    {
        return _context.Categories.Any(e => e.Id == id);
    }
}
