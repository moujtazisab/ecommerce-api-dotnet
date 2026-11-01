
using System.Security.Claims;
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReviewsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin => User.IsInRole("Admin");

    // GET: api/Reviews
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var query = _context.Reviews
            .AsNoTracking()
            .AsQueryable();

        if (!(User.Identity?.IsAuthenticated == true && IsAdmin))
            query = query.Where(r => r.IsApproved);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.ReviewId,
                r.ProductId,
                ProductName = r.Product.Name,
                r.UserId,
                UserName = r.User.FirstName + " " + r.User.LastName,
                r.Rating,
                r.Comment,
                r.IsApproved,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    // GET: api/Reviews/1
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var query = _context.Reviews
            .AsNoTracking()
            .Where(r => r.ReviewId == id);

        if (!(User.Identity?.IsAuthenticated == true && IsAdmin))
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                query = query.Where(r =>
                    r.IsApproved || r.UserId == CurrentUserId);
            }
            else
            {
                query = query.Where(r => r.IsApproved);
            }
        }

        var review = await query
            .Select(r => new
            {
                r.ReviewId,
                r.ProductId,
                ProductName = r.Product.Name,
                r.UserId,
                UserName = r.User.FirstName + " " + r.User.LastName,
                r.Rating,
                r.Comment,
                r.IsApproved,
                r.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (review == null)
            return NotFound(new { message = "Review not found." });

        return Ok(review);
    }

    // POST: api/Reviews
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Rating must be between 1 and 5." });

        if (request.Comment?.Length > 1000)
            return BadRequest(new { message = "Comment cannot exceed 1000 characters." });

        var productExists = await _context.Products
            .AnyAsync(p => p.ProductId == request.ProductId);

        if (!productExists)
            return NotFound(new { message = "Product not found." });

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r =>
                r.UserId == CurrentUserId &&
                r.ProductId == request.ProductId);

        if (alreadyReviewed)
            return Conflict(new
            {
                message = "You have already reviewed this product."
            });

        var review = new Review
        {
            UserId = CurrentUserId,
            ProductId = request.ProductId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim(),
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                message = "A review already exists for this product."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = review.ReviewId },
            new
            {
                review.ReviewId,
                review.ProductId,
                review.Rating,
                review.Comment,
                review.IsApproved,
                review.CreatedAt
            }
        );
    }

    // PUT: api/Reviews/1
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(
        int id,
        UpdateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { message = "Rating must be between 1 and 5." });

        if (request.Comment?.Length > 1000)
            return BadRequest(new { message = "Comment cannot exceed 1000 characters." });

        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (review == null)
            return NotFound(new { message = "Review not found." });

        if (!IsAdmin && review.UserId != CurrentUserId)
            return Forbid();

        review.Rating = request.Rating;
        review.Comment = request.Comment?.Trim();

        // Client modifications require moderation again.
        if (!IsAdmin)
            review.IsApproved = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH: api/Reviews/1/approval
    [HttpPatch("{id}/approval")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateApproval(
        int id,
        UpdateReviewApprovalRequest request)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (review == null)
            return NotFound(new { message = "Review not found." });

        review.IsApproved = request.IsApproved;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Review moderation updated.",
            review.ReviewId,
            review.IsApproved
        });
    }

    // DELETE: api/Reviews/1
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ReviewId == id);

        if (review == null)
            return NotFound(new { message = "Review not found." });

        if (!IsAdmin && review.UserId != CurrentUserId)
            return Forbid();

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateReviewRequest(
    int ProductId,
    byte Rating,
    string? Comment
);

public record UpdateReviewRequest(
    byte Rating,
    string? Comment
);

public record UpdateReviewApprovalRequest(
    bool IsApproved
);
