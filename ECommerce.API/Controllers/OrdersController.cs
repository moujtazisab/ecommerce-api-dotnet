
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
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: api/Orders
    // Admin only
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Select(o => new
            {
                o.OrderId,
                o.OrderNumber,
                o.UserId,
                CustomerName = o.User.FirstName + " " + o.User.LastName,
                o.OrderDate,
                o.TotalQuantity,
                o.SubTotal,
                o.DiscountAmount,
                o.ShippingAmount,
                o.TotalAmount,
                o.DeliveryType,
                o.Status
            })
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return Ok(orders);
    }

    // GET: api/Orders/1
    // Admin can access all orders.
    // Client can access only their own orders.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == id);

        if (!IsAdmin)
            query = query.Where(o => o.UserId == CurrentUserId);

        var order = await query
            .Select(o => new
            {
                o.OrderId,
                o.OrderNumber,
                o.UserId,
                o.OrderDate,
                o.TotalQuantity,
                o.SubTotal,
                o.DiscountAmount,
                o.ShippingAmount,
                o.TotalAmount,
                o.DeliveryType,
                o.ShippingAddress,
                o.ShippingCity,
                o.ShippingPostalCode,
                o.ShippingCountry,
                o.Status,
                o.Notes,

                Items = o.OrderItems.Select(i => new
                {
                    i.OrderItemId,
                    i.ProductId,
                    i.ProductName,
                    i.Sku,
                    i.OriginalPrice,
                    i.UnitPrice,
                    i.DiscountAmount,
                    i.Quantity,
                    i.TotalPrice
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "Order not found." });

        return Ok(order);
    }

    // POST: api/Orders
    // Client creates an order from their own cart.
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeliveryType))
            return BadRequest(new
            {
                message = "Delivery type is required."
            });

        if (request.DeliveryType.Trim().Length > 20)
            return BadRequest(new
            {
                message = "Delivery type cannot exceed 20 characters."
            });

        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            return BadRequest(new
            {
                message = "Shipping address is required."
            });

        if (request.ShippingAddress.Trim().Length > 500)
            return BadRequest(new
            {
                message = "Shipping address cannot exceed 500 characters."
            });

        if (string.IsNullOrWhiteSpace(request.ShippingCity))
            return BadRequest(new
            {
                message = "Shipping city is required."
            });

        if (request.ShippingCity.Trim().Length > 100)
            return BadRequest(new
            {
                message = "Shipping city cannot exceed 100 characters."
            });

        if (request.ShippingPostalCode?.Length > 20)
            return BadRequest(new
            {
                message = "Postal code cannot exceed 20 characters."
            });

        if (request.ShippingCountry?.Length > 100)
            return BadRequest(new
            {
                message = "Country cannot exceed 100 characters."
            });

        if (request.Notes?.Length > 500)
            return BadRequest(new
            {
                message = "Notes cannot exceed 500 characters."
            });

        var userId = CurrentUserId;

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
            return BadRequest(new
            {
                message = "Your cart is empty."
            });

        // Validate products and quantities.
        foreach (var item in cart.CartItems)
        {
            if (item.Quantity <= 0)
                return BadRequest(new
                {
                    message = "Invalid cart quantity."
                });

            if (!item.Product.IsActive ||
                !item.Product.IsAvailable)
            {
                return BadRequest(new
                {
                    message = $"Product {item.Product.Name} is unavailable."
                });
            }

            if (item.Quantity > item.Product.Quantity)
            {
                return BadRequest(new
                {
                    message = $"Insufficient stock for {item.Product.Name}."
                });
            }
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            decimal subTotal = 0;
            int totalQuantity = 0;

            var order = new Order
            {
                UserId = userId,

                OrderNumber =
                    $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}",

                OrderDate = DateTime.UtcNow,

                DeliveryType = request.DeliveryType.Trim(),
                ShippingAddress = request.ShippingAddress.Trim(),
                ShippingCity = request.ShippingCity.Trim(),
                ShippingPostalCode = request.ShippingPostalCode?.Trim(),
                ShippingCountry = request.ShippingCountry?.Trim(),
                Notes = request.Notes?.Trim(),

                Status = "PENDING",
                DiscountAmount = 0,
                ShippingAmount = 0
            };

            foreach (var cartItem in cart.CartItems)
            {
                var product = cartItem.Product;

                var unitPrice = product.Price;
                var totalPrice = unitPrice * cartItem.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    OriginalPrice = product.OriginalPrice,
                    UnitPrice = unitPrice,
                    DiscountAmount = 0,
                    Quantity = cartItem.Quantity,
                    TotalPrice = totalPrice
                };

                order.OrderItems.Add(orderItem);

                subTotal += totalPrice;
                totalQuantity += cartItem.Quantity;

                product.Quantity -= cartItem.Quantity;
            }

            order.SubTotal = subTotal;
            order.TotalQuantity = totalQuantity;

            order.TotalAmount =
                order.SubTotal -
                order.DiscountAmount +
                order.ShippingAmount;

            // Save order and order items.
            _context.Orders.Add(order);

            // Remove cart items after successful order creation.
            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = order.OrderId },
                new
                {
                    message = "Order created successfully.",
                    order.OrderId,
                    order.OrderNumber,
                    order.TotalQuantity,
                    order.SubTotal,
                    order.TotalAmount,
                    order.Status
                }
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // PATCH: api/Orders/1/status
    // Admin changes order status.
    // Every change is automatically recorded in OrderStatusHistory.
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdateOrderStatusRequest request)
    {
        var allowedStatuses = new[]
        {
            "PENDING",
            "CONFIRMED",
            "PAID",
            "PROCESSING",
            "SHIPPED",
            "DELIVERED",
            "CANCELLED"
        };

        var newStatus = request.Status?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(newStatus) ||
            !allowedStatuses.Contains(newStatus))
        {
            return BadRequest(new
            {
                message = "Invalid order status."
            });
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
            return NotFound(new
            {
                message = "Order not found."
            });

        var oldStatus = order.Status;

        // Avoid duplicate history records.
        if (oldStatus == newStatus)
        {
            return Ok(new
            {
                message = "Order already has this status.",
                order.OrderId,
                order.Status
            });
        }

        // Update order status.
        order.Status = newStatus;

        // Automatically create history record.
        var history = new OrderStatusHistory
        {
            OrderId = order.OrderId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = CurrentUserId,
            Comment = request.Comment?.Trim(),
            ChangedAt = DateTime.UtcNow
        };

        _context.OrderStatusHistories.Add(history);

        // Save order and history together.
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Order status updated and history recorded.",
            order.OrderId,
            OldStatus = oldStatus,
            NewStatus = newStatus
        });
    }

    // DELETE: api/Orders/1
    // Orders must not be physically deleted.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
            return NotFound(new
            {
                message = "Order not found."
            });

        return Conflict(new
        {
            message = "Orders cannot be deleted. Use the cancellation workflow instead."
        });
    }
}

// DTOs

public record CreateOrderRequest(
    string DeliveryType,
    string ShippingAddress,
    string ShippingCity,
    string? ShippingPostalCode,
    string? ShippingCountry,
    string? Notes
);

public record UpdateOrderStatusRequest(
    string Status,
    string? Comment
);
