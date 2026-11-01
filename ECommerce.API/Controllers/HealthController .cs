using ECommerce.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var databaseConnected = await _context.Database.CanConnectAsync();

        return Ok(new
        {
            ApiStatus = "Healthy",
            DatabaseStatus = databaseConnected ? "Connected" : "Disconnected",
            Timestamp = DateTime.UtcNow
        });
    }
}