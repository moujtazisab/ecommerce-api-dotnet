
using System.Security.Claims;
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CartsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin =>
        User.IsInRole("Admin");

    // GET: api/Carts
    // Admin only
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var carts = await _context.Carts
            .AsNoTracking()
            .Include(c => c.CartItems)
            .ToListAsync();

        return Ok(carts);
    }

    // GET: api/Carts/1
    // Admin or owner
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = _context.Carts
            .AsNoTracking()
            .Include(c => c.CartItems)
            .Where(c => c.CartId == id);

        if (!IsAdmin)
            query = query.Where(c => c.UserId == CurrentUserId);

        var cart = await query.FirstOrDefaultAsync();

        if (cart == null)
            return NotFound(new { message = "Cart not found." });

        return Ok(cart);
    }

    // POST: api/Carts
    // A client can only create their own cart
    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var userId = CurrentUserId;

        var existingCart = await _context.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (existingCart != null)
            return Conflict(new { message = "User already has a cart." });

        var cart = new Cart
        {
            UserId = userId
        };

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = cart.CartId },
            new
            {
                cart.CartId,
                cart.UserId
            }
        );
    }

    // PUT: api/Carts/1
    // Cart ownership cannot be changed here.
    // Cart contents are managed through CartItemsController.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Cart cart)
    {
        if (id != cart.CartId)
            return BadRequest(new { message = "ID mismatch." });

        var existingCart = await _context.Carts
            .FirstOrDefaultAsync(c => c.CartId == id);

        if (existingCart == null)
            return NotFound(new { message = "Cart not found." });

        if (!IsAdmin && existingCart.UserId != CurrentUserId)
            return Forbid();

        if (cart.UserId != existingCart.UserId)
        {
            return BadRequest(new
            {
                message = "Cart ownership cannot be changed."
            });
        }

        return NoContent();
    }

    // DELETE: api/Carts/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.CartId == id);

        if (cart == null)
            return NotFound(new { message = "Cart not found." });

        if (!IsAdmin && cart.UserId != CurrentUserId)
            return Forbid();

        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
