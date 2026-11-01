
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Categories
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .ToListAsync();

        return Ok(categories);
    }

    // GET: api/Categories/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        return Ok(category);
    }

    // POST: api/Categories
    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            return BadRequest(new { message = "Category name is required." });

        var name = category.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "Category already exists." });

        category.Name = name;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.CategoryId },
            category
        );
    }

    // PUT: api/Categories/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Category category)
    {
        if (id != category.CategoryId)
            return BadRequest(new { message = "ID mismatch." });

        if (string.IsNullOrWhiteSpace(category.Name))
            return BadRequest(new { message = "Category name is required." });

        var existingCategory = await _context.Categories.FindAsync(id);

        if (existingCategory == null)
            return NotFound(new { message = "Category not found." });

        var name = category.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(c => c.CategoryId != id &&
                           c.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "Category already exists." });

        existingCategory.Name = name;
        existingCategory.Description = category.Description;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Categories/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        var hasSubCategories = await _context.SubCategories
            .AnyAsync(s => s.CategoryId == id);

        if (hasSubCategories)
        {
            return Conflict(new
            {
                message = "Cannot delete a category that contains subcategories."
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
