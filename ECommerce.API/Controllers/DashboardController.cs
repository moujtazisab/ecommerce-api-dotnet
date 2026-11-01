
using ECommerce.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Dashboard/statistics
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var statistics = new
        {
            TotalUsers = await _context.Users.CountAsync(),

            // RoleId 2 = Client
            TotalClients = await _context.Users
                .CountAsync(u => u.RoleId == 2),

            TotalProducts = await _context.Products.CountAsync(),

            TotalCategories = await _context.Categories.CountAsync(),

            TotalSubCategories = await _context.SubCategories.CountAsync(),

            TotalSuppliers = await _context.Suppliers.CountAsync(),

            TotalOrders = await _context.Orders.CountAsync(),

            TotalReviews = await _context.Reviews.CountAsync(),

            TotalCarts = await _context.Carts.CountAsync(),

            TotalPayments = await _context.Payments.CountAsync(),

            TotalShipments = await _context.Shipments.CountAsync(),

            // Revenue based only on successfully paid payments.
            TotalRevenue = await _context.Payments
                .Where(p => p.Status == "PAID")
                .SumAsync(p => (decimal?)p.Amount) ?? 0
        };

        return Ok(statistics);
    }

    // GET: api/Dashboard/recent-orders
    [HttpGet("recent-orders")]
    public async Task<IActionResult> GetRecentOrders()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => new
            {
                o.OrderId,
                o.OrderNumber,
                o.UserId,
                CustomerName = o.User.FirstName + " " + o.User.LastName,
                o.OrderDate,
                o.TotalAmount,
                o.Status
            })
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/Dashboard/recent-reviews
    [HttpGet("recent-reviews")]
    public async Task<IActionResult> GetRecentReviews()
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new
            {
                r.ReviewId,
                r.ProductId,
                ProductName = r.Product != null
                    ? r.Product.Name
                    : null,
                r.UserId,
                UserName = r.User != null
                    ? r.User.FirstName + " " + r.User.LastName
                    : null,
                r.Rating,
                r.Comment,
                r.IsApproved,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }
}
