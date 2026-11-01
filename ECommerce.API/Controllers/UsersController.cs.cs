
using ECommerce.API.Data;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UsersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Phone,
                u.RoleId,
                u.Address,
                u.City,
                u.PostalCode,
                u.Country,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/Users/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == id)
            .Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Phone,
                u.RoleId,
                u.Address,
                u.City,
                u.PostalCode,
                u.Country,
                u.IsActive,
                u.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    // POST: api/Users
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest(new
            {
                message = "First name, last name, email and password are required."
            });
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return Conflict(new { message = "Email already exists." });
        }

        if (request.RoleId != 1 && request.RoleId != 2)
        {
            return BadRequest(new { message = "Invalid role." });
        }

        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);

        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            request.Password,
            salt,
            100000,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            32
        );

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}",
            Phone = request.Phone,
            RoleId = request.RoleId,
            Address = request.Address,
            City = request.City,
            PostalCode = request.PostalCode,
            Country = request.Country,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.UserId },
            new
            {
                user.UserId,
                user.FirstName,
                user.LastName,
                user.Email,
                user.RoleId
            }
        );
    }

    // PUT: api/Users/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(
            u => u.Email == email && u.UserId != id))
        {
            return Conflict(new { message = "Email already exists." });
        }

        if (request.RoleId != 1 && request.RoleId != 2)
        {
            return BadRequest(new { message = "Invalid role." });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.Phone = request.Phone;
        user.RoleId = request.RoleId;
        user.Address = request.Address;
        user.City = request.City;
        user.PostalCode = request.PostalCode;
        user.Country = request.Country;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Users/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (user.RoleId == 1)
        {
            return BadRequest(new
            {
                message = "Admin accounts cannot be deleted through this endpoint."
            });
        }

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    byte RoleId,
    string? Phone,
    string? Address,
    string? City,
    string? PostalCode,
    string? Country
);

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    byte RoleId,
    string? Phone,
    string? Address,
    string? City,
    string? PostalCode,
    string? Country,
    bool IsActive
);
