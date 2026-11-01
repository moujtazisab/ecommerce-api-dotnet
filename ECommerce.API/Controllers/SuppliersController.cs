
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuppliersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Suppliers
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .ToListAsync();

        return Ok(suppliers);
    }

    // GET: api/Suppliers/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null)
            return NotFound(new { message = "Supplier not found." });

        return Ok(supplier);
    }

    // POST: api/Suppliers
    [HttpPost]
    public async Task<IActionResult> Create(Supplier supplier)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(supplier.Name))
            return BadRequest(new { message = "Supplier name is required." });

        supplier.Name = supplier.Name.Trim();

        var exists = await _context.Suppliers
            .AnyAsync(s => s.Name.ToLower() == supplier.Name.ToLower());

        if (exists)
            return Conflict(new { message = "Supplier already exists." });

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = supplier.SupplierId },
            supplier
        );
    }

    // PUT: api/Suppliers/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Supplier supplier)
    {
        if (id != supplier.SupplierId)
            return BadRequest(new { message = "ID mismatch." });

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(supplier.Name))
            return BadRequest(new { message = "Supplier name is required." });

        var existingSupplier = await _context.Suppliers.FindAsync(id);

        if (existingSupplier == null)
            return NotFound(new { message = "Supplier not found." });

        var name = supplier.Name.Trim();

        var exists = await _context.Suppliers.AnyAsync(s =>
            s.SupplierId != id &&
            s.Name.ToLower() == name.ToLower());

        if (exists)
            return Conflict(new { message = "Supplier already exists." });

        // Update existing entity without replacing its relationships.
        existingSupplier.Name = name;

        // Add other editable supplier properties here if your model has them.

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Suppliers/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
            return NotFound(new { message = "Supplier not found." });

        var hasProducts = await _context.Products
            .AnyAsync(p => p.SupplierId == id);

        if (hasProducts)
        {
            return Conflict(new
            {
                message = "Cannot delete a supplier associated with products."
            });
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
