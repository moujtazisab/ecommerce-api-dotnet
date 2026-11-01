
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentMethodsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PaymentMethodsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/PaymentMethods
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var query = _context.PaymentMethods.AsNoTracking();

        if (!(User.Identity?.IsAuthenticated == true &&
              User.IsInRole("Admin")))
        {
            query = query.Where(pm => pm.IsActive);
        }

        var methods = await query
            .Select(pm => new
            {
                pm.PaymentMethodId,
                pm.Name,
                pm.Code,
                pm.IsActive
            })
            .ToListAsync();

        return Ok(methods);
    }

    // GET: api/PaymentMethods/1
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var query = _context.PaymentMethods
            .AsNoTracking()
            .Where(pm => pm.PaymentMethodId == id);

        if (!(User.Identity?.IsAuthenticated == true &&
              User.IsInRole("Admin")))
        {
            query = query.Where(pm => pm.IsActive);
        }

        var method = await query
            .Select(pm => new
            {
                pm.PaymentMethodId,
                pm.Name,
                pm.Code,
                pm.IsActive
            })
            .FirstOrDefaultAsync();

        if (method == null)
            return NotFound(new { message = "Payment method not found." });

        return Ok(method);
    }

    // POST: api/PaymentMethods
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreatePaymentMethodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Name and code are required." });
        }

        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        var exists = await _context.PaymentMethods
            .AnyAsync(pm => pm.Code == code);

        if (exists)
            return Conflict(new { message = "Payment method code already exists." });

        var method = new PaymentMethod
        {
            Name = name,
            Code = code,
            IsActive = true
        };

        _context.PaymentMethods.Add(method);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = method.PaymentMethodId },
            new
            {
                method.PaymentMethodId,
                method.Name,
                method.Code,
                method.IsActive
            }
        );
    }

    // PUT: api/PaymentMethods/1
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        UpdatePaymentMethodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Name and code are required." });
        }

        var method = await _context.PaymentMethods
            .FirstOrDefaultAsync(pm => pm.PaymentMethodId == id);

        if (method == null)
            return NotFound(new { message = "Payment method not found." });

        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        var duplicate = await _context.PaymentMethods
            .AnyAsync(pm =>
                pm.PaymentMethodId != id &&
                pm.Code == code);

        if (duplicate)
            return Conflict(new { message = "Payment method code already exists." });

        method.Name = name;
        method.Code = code;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/PaymentMethods/1/activate
    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id)
    {
        var method = await _context.PaymentMethods.FindAsync(id);

        if (method == null)
            return NotFound(new { message = "Payment method not found." });

        method.IsActive = true;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Payment method activated." });
    }

    // DELETE: api/PaymentMethods/1
    // Soft delete: deactivate instead of physically deleting.
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var method = await _context.PaymentMethods.FindAsync(id);

        if (method == null)
            return NotFound(new { message = "Payment method not found." });

        method.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Payment method deactivated." });
    }
}

public record CreatePaymentMethodRequest(
    string Name,
    string Code
);

public record UpdatePaymentMethodRequest(
    string Name,
    string Code
);
