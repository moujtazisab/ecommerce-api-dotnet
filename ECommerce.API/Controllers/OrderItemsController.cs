
using System.Security.Claims;
using ECommerce.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderItemsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrderItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: api/OrderItems
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var items = await _context.OrderItems
            .AsNoTracking()
            .Select(oi => new
            {
                oi.OrderItemId,
                oi.OrderId,
                oi.ProductId,
                oi.ProductName,
                oi.Sku,
                oi.OriginalPrice,
                oi.UnitPrice,
                oi.DiscountAmount,
                oi.Quantity,
                oi.TotalPrice,
                oi.Order.UserId
            })
            .ToListAsync();

        return Ok(items);
    }

    // GET: api/OrderItems/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var query = _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.OrderItemId == id);

        if (!IsAdmin)
            query = query.Where(oi => oi.Order.UserId == CurrentUserId);

        var item = await query
            .Select(oi => new
            {
                oi.OrderItemId,
                oi.OrderId,
                oi.ProductId,
                oi.ProductName,
                oi.Sku,
                oi.OriginalPrice,
                oi.UnitPrice,
                oi.DiscountAmount,
                oi.Quantity,
                oi.TotalPrice
            })
            .FirstOrDefaultAsync();

        if (item == null)
            return NotFound(new { message = "Order item not found." });

        return Ok(item);
    }
}
