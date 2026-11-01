
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
public class OrderCancellationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    private static readonly string[] ValidRefundStatuses =
    {
        "PENDING",
        "APPROVED",
        "REJECTED",
        "REFUNDED"
    };

    public OrderCancellationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: api/OrderCancellations
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var query = _context.OrderCancellations
            .AsNoTracking()
            .AsQueryable();

        if (!IsAdmin)
            query = query.Where(c => c.UserId == CurrentUserId);

        var cancellations = await query
            .Select(c => new
            {
                c.CancellationId,
                c.OrderId,
                c.UserId,
                c.Reason,
                c.CancellationDate,
                c.RefundAmount,
                c.RefundStatus
            })
            .ToListAsync();

        return Ok(cancellations);
    }

    // GET: api/OrderCancellations/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cancellation = await _context.OrderCancellations
            .AsNoTracking()
            .Where(c => c.CancellationId == id)
            .Where(c => IsAdmin || c.UserId == CurrentUserId)
            .Select(c => new
            {
                c.CancellationId,
                c.OrderId,
                c.UserId,
                c.Reason,
                c.CancellationDate,
                c.RefundAmount,
                c.RefundStatus
            })
            .FirstOrDefaultAsync();

        if (cancellation == null)
            return NotFound(new { message = "Cancellation not found." });

        return Ok(cancellation);
    }

    // POST: api/OrderCancellations
    [HttpPost]
    public async Task<IActionResult> Create(CreateCancellationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Cancellation reason is required." });

        if (request.Reason.Trim().Length > 500)
            return BadRequest(new { message = "Reason cannot exceed 500 characters." });

        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

        if (order == null)
            return NotFound(new { message = "Order not found." });

        // A client can only cancel their own order.
        if (!IsAdmin && order.UserId != CurrentUserId)
            return Forbid();

        // Only orders not yet shipped or delivered can be cancelled.
        var eligibleStatuses = new[]
        {
            "PENDING",
            "CONFIRMED",
            "PAID",
            "PROCESSING"
        };

        if (!eligibleStatuses.Contains(order.Status))
            return BadRequest(new
            {
                message = "This order can no longer be cancelled."
            });

        var alreadyCancelled = await _context.OrderCancellations
            .AnyAsync(c => c.OrderId == request.OrderId);

        if (alreadyCancelled)
            return Conflict(new
            {
                message = "A cancellation request already exists for this order."
            });

        var cancellation = new OrderCancellation
        {
            OrderId = order.OrderId,
            UserId = order.UserId,
            Reason = request.Reason.Trim(),
            CancellationDate = DateTime.UtcNow,
            RefundAmount = order.TotalAmount,
            RefundStatus = "PENDING"
        };

        order.Status = "CANCELLED";

        _context.OrderCancellations.Add(cancellation);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = cancellation.CancellationId },
            new
            {
                cancellation.CancellationId,
                cancellation.OrderId,
                cancellation.Reason,
                cancellation.CancellationDate,
                cancellation.RefundAmount,
                cancellation.RefundStatus
            }
        );
    }

    // PATCH: api/OrderCancellations/1/refund-status
    [HttpPatch("{id}/refund-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRefundStatus(
        int id,
        UpdateRefundStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefundStatus))
            return BadRequest(new { message = "Refund status is required." });

        var status = request.RefundStatus.Trim().ToUpperInvariant();

        if (!ValidRefundStatuses.Contains(status))
            return BadRequest(new { message = "Invalid refund status." });

        var cancellation = await _context.OrderCancellations
            .FirstOrDefaultAsync(c => c.CancellationId == id);

        if (cancellation == null)
            return NotFound(new { message = "Cancellation not found." });

        cancellation.RefundStatus = status;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Refund status updated.",
            cancellation.CancellationId,
            cancellation.RefundStatus
        });
    }
}

public record CreateCancellationRequest(
    int OrderId,
    string Reason
);

public record UpdateRefundStatusRequest(
    string RefundStatus
);
