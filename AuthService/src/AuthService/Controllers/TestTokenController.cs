using AuthService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

/// <summary>
/// Test-only endpoint for retrieving email tokens without an SMTP server.
/// Only active when EnableTestEndpoints=true in configuration.
/// Never deploy with this enabled in production.
/// </summary>
[ApiController]
[Route("api/test")]
public class TestTokenController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly IConfiguration _config;

    public TestTokenController(AuthDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private bool IsEnabled() =>
        string.Equals(_config["EnableTestEndpoints"], "true", StringComparison.OrdinalIgnoreCase);

    [HttpGet("tokens/invite")]
    public async Task<IActionResult> GetInviteToken([FromQuery] string email)
    {
        if (!IsEnabled()) return NotFound();

        var token = await _db.InviteTokens
            .Where(t => t.Email == email && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.Token)
            .FirstOrDefaultAsync();

        if (token is null) return NotFound();
        return Ok(new { token });
    }

    [HttpGet("tokens/password-reset")]
    public async Task<IActionResult> GetPasswordResetToken([FromQuery] string email)
    {
        if (!IsEnabled()) return NotFound();

        var token = await _db.PasswordResetTokens
            .Where(t => t.Email == email && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => t.Token)
            .FirstOrDefaultAsync();

        if (token is null) return NotFound();
        return Ok(new { token });
    }
}
