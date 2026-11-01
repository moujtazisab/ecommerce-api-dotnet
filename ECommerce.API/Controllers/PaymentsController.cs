
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
public class PaymentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PaymentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: api/Payments
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _context.Payments
            .AsNoTracking()
            .Select(p => new
            {
                p.PaymentId,
                p.OrderId,
                p.Order.OrderNumber,
                p.Order.UserId,
                p.PaymentMethodId,
                PaymentMethod = p.PaymentMethod.Name,
                p.Amount,
                p.Status,
                p.TransactionReference,
                p.PaymentDate,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(payments);
    }

    // GET: api/Payments/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentId == id);

        if (!IsAdmin)
            query = query.Where(p => p.Order.UserId == CurrentUserId);

        var payment = await query
            .Select(p => new
            {
                p.PaymentId,
                p.OrderId,
                p.Order.OrderNumber,
                p.PaymentMethodId,
                PaymentMethod = p.PaymentMethod.Name,
                p.Amount,
                p.Status,
                p.TransactionReference,
                p.PaymentDate,
                p.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (payment == null)
            return NotFound(new { message = "Payment not found." });

        return Ok(payment);
    }

    // POST: api/Payments
    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentRequest request)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        if (!IsAdmin && order.UserId != CurrentUserId)
            return Forbid();

        if (order.Status == "CANCELLED")
            return BadRequest(new { message = "Cannot pay for a cancelled order." });

        var existingPayment = await _context.Payments
            .AnyAsync(p => p.OrderId == request.OrderId);

        if (existingPayment)
            return Conflict(new { message = "A payment already exists for this order." });

        var paymentMethod = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm =>
                pm.PaymentMethodId == request.PaymentMethodId &&
                pm.IsActive);

        if (paymentMethod == null)
            return BadRequest(new { message = "Payment method is unavailable." });

        var payment = new Payment
        {
            OrderId = order.OrderId,
            PaymentMethodId = paymentMethod.PaymentMethodId,
            Amount = order.TotalAmount,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = payment.PaymentId },
            new
            {
                payment.PaymentId,
                payment.OrderId,
                payment.Amount,
                payment.Status,
                PaymentMethod = paymentMethod.Name
            }
        );
    }

    // PATCH: api/Payments/1/status
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        UpdatePaymentStatusRequest request)
    {
        var allowedStatuses = new[]
        {
            "PENDING",
            "PAID",
            "FAILED",
            "REFUNDED"
        };

        var newStatus = request.Status?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(newStatus) ||
            !allowedStatuses.Contains(newStatus))
        {
            return BadRequest(new { message = "Invalid payment status." });
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentId == id);

        if (payment == null)
            return NotFound(new { message = "Payment not found." });

        payment.Status = newStatus;

        if (newStatus == "PAID" && payment.PaymentDate == null)
            payment.PaymentDate = DateTime.UtcNow;

        if (newStatus == "REFUNDED")
            payment.PaymentDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Payment status updated successfully.",
            payment.PaymentId,
            payment.Status,
            payment.PaymentDate
        });
    }

    // DELETE is intentionally disabled.
    // Payment records must be preserved for financial traceability.
}

public record CreatePaymentRequest(
    int OrderId,
    int PaymentMethodId
);

public record UpdatePaymentStatusRequest(string Status);
