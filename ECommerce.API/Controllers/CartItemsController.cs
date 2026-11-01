
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
public class CartItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CartItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: api/CartItems
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var items = await _context.CartItems
            .AsNoTracking()
            .Select(ci => new
            {
                ci.CartItemId,
                ci.CartId,
                ci.ProductId,
                ci.Quantity,
                ci.Cart.UserId,
                ProductName = ci.Product.Name,
                ProductPrice = ci.Product.Price
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/CartItems/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = _context.CartItems
            .AsNoTracking()
            .Where(ci => ci.CartItemId == id);

        if (!IsAdmin)
            query = query.Where(ci => ci.Cart.UserId == CurrentUserId);

        var item = await query
            .Select(ci => new
            {
                ci.CartItemId,
                ci.CartId,
                ci.ProductId,
                ci.Quantity,
                ProductName = ci.Product.Name,
                ProductPrice = ci.Product.Price,
                ProductImage = ci.Product.ImageUrl
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = "Cart item not found." });

        return Ok(item);
    }

    // POST: api/CartItems
    [HttpPost]
    public async Task<IActionResult> Create(CreateCartItemRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero." });

        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.CartId == request.CartId);

        if (cart == null)
            return NotFound(new { message = "Cart not found." });

        if (!IsAdmin && cart.UserId != CurrentUserId)
            return Forbid();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == request.ProductId);

        if (product == null || !product.IsActive || !product.IsAvailable)
            return NotFound(new { message = "Product not found or unavailable." });

        if (request.Quantity > product.Quantity)
            return BadRequest(new { message = "Insufficient stock." });

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(ci =>
                ci.CartId == request.CartId &&
                ci.ProductId == request.ProductId);

        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;

            if (newQuantity > product.Quantity)
                return BadRequest(new { message = "Insufficient stock." });

            existingItem.Quantity = newQuantity;
        }
        else
        {
            var cartItem = new CartItem
            {
                CartId = request.CartId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };

            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Product added to cart successfully." });
    }

    // PUT: api/CartItems/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateCartItemRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Quantity must be greater than zero." });

        var query = _context.CartItems
            .Where(ci => ci.CartItemId == id);

        if (!IsAdmin)
            query = query.Where(ci => ci.Cart.UserId == CurrentUserId);

        var item = await query.FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = "Cart item not found." });

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);

        if (product == null || !product.IsActive || !product.IsAvailable)
            return BadRequest(new { message = "Product is unavailable." });

        if (request.Quantity > product.Quantity)
            return BadRequest(new { message = "Insufficient stock." });

        item.Quantity = request.Quantity;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/CartItems/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var query = _context.CartItems
            .Where(ci => ci.CartItemId == id);

        if (!IsAdmin)
            query = query.Where(ci => ci.Cart.UserId == CurrentUserId);

        var item = await query.FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = "Cart item not found." });

        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateCartItemRequest(
    int CartId,
    int ProductId,
    int Quantity
);

public record UpdateCartItemRequest(int Quantity);
