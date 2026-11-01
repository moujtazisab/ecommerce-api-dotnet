
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DiscountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DiscountsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Discounts
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var discounts = await _context.Discounts
            .AsNoTracking()
            .ToListAsync();

        return Ok(discounts);
    }

    // GET: api/Discounts/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var discount = await _context.Discounts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DiscountId == id);

        if (discount == null)
            return NotFound(new { message = "Discount not found." });

        return Ok(discount);
    }

    // POST: api/Discounts
    [HttpPost]
    public async Task<IActionResult> Create(Discount discount)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = discount.DiscountId },
            discount
        );
    }

    // PUT: api/Discounts/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Discount discount)
    {
        if (id != discount.DiscountId)
            return BadRequest(new { message = "ID mismatch." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingDiscount = await _context.Discounts.FindAsync(id);

        if (existingDiscount == null)
            return NotFound(new { message = "Discount not found." });

        _context.Entry(existingDiscount)
            .CurrentValues.SetValues(discount);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Discounts/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var discount = await _context.Discounts.FindAsync(id);

        if (discount == null)
            return NotFound(new { message = "Discount not found." });

        var isUsedByProducts = await _context.Products
            .AnyAsync(p => p.DiscountId == id);

        if (isUsedByProducts)
        {
            return Conflict(new
            {
                message = "Cannot delete a discount currently assigned to products."
            });
        }

        _context.Discounts.Remove(discount);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
