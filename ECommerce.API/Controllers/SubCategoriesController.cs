
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SubCategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SubCategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/SubCategories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var subCategories = await _context.SubCategories
            .AsNoTracking()
            .Select(s => new
            {
                s.SubCategoryId,
                s.Name,
                s.Description,
                s.CategoryId,
                CategoryName = s.Category != null
                    ? s.Category.Name
                    : null
            })
            .ToListAsync();

        return Ok(subCategories);
    }

    // GET: api/SubCategories/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var subCategory = await _context.SubCategories
            .AsNoTracking()
            .Where(s => s.SubCategoryId == id)
            .Select(s => new
            {
                s.SubCategoryId,
                s.Name,
                s.Description,
                s.CategoryId,
                CategoryName = s.Category != null
                    ? s.Category.Name
                    : null
            })
            .FirstOrDefaultAsync();

        if (subCategory == null)
            return NotFound(new { message = "SubCategory not found." });

        return Ok(subCategory);
    }

    // POST: api/SubCategories
    [HttpPost]
    public async Task<IActionResult> Create(SubCategory subCategory)
    {
        if (string.IsNullOrWhiteSpace(subCategory.Name))
            return BadRequest(new { message = "SubCategory name is required." });

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == subCategory.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "Category not found." });

        var name = subCategory.Name.Trim();

        var exists = await _context.SubCategories.AnyAsync(s =>
            s.CategoryId == subCategory.CategoryId &&
            s.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "SubCategory already exists in this category." });

        subCategory.Name = name;

        _context.SubCategories.Add(subCategory);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = subCategory.SubCategoryId },
            new
            {
                subCategory.SubCategoryId,
                subCategory.Name,
                subCategory.Description,
                subCategory.CategoryId
            }
        );
    }

    // PUT: api/SubCategories/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, SubCategory subCategory)
    {
        if (id != subCategory.SubCategoryId)
            return BadRequest(new { message = "ID mismatch." });

        if (string.IsNullOrWhiteSpace(subCategory.Name))
            return BadRequest(new { message = "SubCategory name is required." });

        var existingSubCategory = await _context.SubCategories.FindAsync(id);

        if (existingSubCategory == null)
            return NotFound(new { message = "SubCategory not found." });

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.CategoryId == subCategory.CategoryId);

        if (!categoryExists)
            return BadRequest(new { message = "Category not found." });

        var name = subCategory.Name.Trim();

        var exists = await _context.SubCategories.AnyAsync(s =>
            s.SubCategoryId != id &&
            s.CategoryId == subCategory.CategoryId &&
            s.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "SubCategory already exists in this category." });

        existingSubCategory.Name = name;
        existingSubCategory.Description = subCategory.Description;
        existingSubCategory.CategoryId = subCategory.CategoryId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/SubCategories/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var subCategory = await _context.SubCategories.FindAsync(id);

        if (subCategory == null)
            return NotFound(new { message = "SubCategory not found." });

        var hasProducts = await _context.Products
            .AnyAsync(p => p.SubCategoryId == id);

        if (hasProducts)
        {
            return Conflict(new
            {
                message = "Cannot delete a subcategory that contains products."
            });
        }

        _context.SubCategories.Remove(subCategory);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
