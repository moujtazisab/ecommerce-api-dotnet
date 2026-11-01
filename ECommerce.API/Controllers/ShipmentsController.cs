
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
public class ShipmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    private static readonly string[] ValidStatuses =
    {
        "PREPARING",
        "SHIPPED",
        "IN_TRANSIT",
        "DELIVERED",
        "FAILED"
    };

    public ShipmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: api/Shipments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _context.Shipments
            .AsNoTracking()
            .Include(s => s.Order)
            .AsQueryable();

        if (!IsAdmin)
            query = query.Where(s => s.Order.UserId == CurrentUserId);

        var shipments = await query
            .Select(s => new
            {
                s.ShipmentId,
                s.OrderId,
                s.Order.OrderNumber,
                s.TrackingNumber,
                s.Carrier,
                s.ShippingDate,
                s.DeliveryDate,
                s.Status
            })
            .ToListAsync();

        return Ok(shipments);
    }

    // GET: api/Shipments/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var shipment = await _context.Shipments
            .AsNoTracking()
            .Where(s => s.ShipmentId == id)
            .Where(s => IsAdmin || s.Order.UserId == CurrentUserId)
            .Select(s => new
            {
                s.ShipmentId,
                s.OrderId,
                s.Order.OrderNumber,
                s.TrackingNumber,
                s.Carrier,
                s.ShippingDate,
                s.DeliveryDate,
                s.Status
            })
            .FirstOrDefaultAsync();

        if (shipment == null)
            return NotFound(new { message = "Shipment not found." });

        return Ok(shipment);
    }

    // POST: api/Shipments
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateShipmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status) ||
            !ValidStatuses.Contains(request.Status.Trim().ToUpperInvariant()))
        {
            return BadRequest(new { message = "Invalid shipment status." });
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        var shipmentExists = await _context.Shipments
            .AnyAsync(s => s.OrderId == request.OrderId);

        if (shipmentExists)
            return Conflict(new { message = "This order already has a shipment." });

        var shipment = new Shipment
        {
            OrderId = request.OrderId,
            TrackingNumber = request.TrackingNumber?.Trim(),
            Carrier = request.Carrier?.Trim(),
            Status = request.Status.Trim().ToUpperInvariant(),
            ShippingDate = request.ShippingDate,
            DeliveryDate = request.DeliveryDate
        };

        _context.Shipments.Add(shipment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = shipment.ShipmentId },
            new
            {
                shipment.ShipmentId,
                shipment.OrderId,
                shipment.TrackingNumber,
                shipment.Carrier,
                shipment.Status
            }
        );
    }

    // PUT: api/Shipments/1
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        UpdateShipmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status) ||
            !ValidStatuses.Contains(request.Status.Trim().ToUpperInvariant()))
        {
            return BadRequest(new { message = "Invalid shipment status." });
        }

        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.ShipmentId == id);

        if (shipment == null)
            return NotFound(new { message = "Shipment not found." });

        shipment.TrackingNumber = request.TrackingNumber?.Trim();
        shipment.Carrier = request.Carrier?.Trim();
        shipment.Status = request.Status.Trim().ToUpperInvariant();
        shipment.ShippingDate = request.ShippingDate;
        shipment.DeliveryDate = request.DeliveryDate;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Shipments/1
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var shipment = await _context.Shipments.FindAsync(id);

        if (shipment == null)
            return NotFound(new { message = "Shipment not found." });

        _context.Shipments.Remove(shipment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateShipmentRequest(
    int OrderId,
    string? TrackingNumber,
    string? Carrier,
    string Status,
    DateTime? ShippingDate,
    DateTime? DeliveryDate
);

public record UpdateShipmentRequest(
    string? TrackingNumber,
    string? Carrier,
    string Status,
    DateTime? ShippingDate,
    DateTime? DeliveryDate
);
