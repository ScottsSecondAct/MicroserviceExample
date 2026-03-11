using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportingService.Data;

namespace ReportingService.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportingDbContext _db;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ReportingDbContext db, ILogger<ReportsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipeline()
    {
        try
        {
            var projections = await _db.PipelineProjections
                .OrderBy(p => p.Stage)
                .ToListAsync();
            return Ok(projections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pipeline report");
            return StatusCode(500, "An error occurred retrieving the pipeline report.");
        }
    }

    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities()
    {
        try
        {
            var projections = await _db.ActivityRepProjections
                .OrderByDescending(a => a.TotalCount)
                .ToListAsync();
            return Ok(projections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity report");
            return StatusCode(500, "An error occurred retrieving the activity report.");
        }
    }

    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts()
    {
        try
        {
            var projections = await _db.ContactFunnelProjections
                .OrderBy(c => c.Status)
                .ToListAsync();
            return Ok(projections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contact funnel report");
            return StatusCode(500, "An error occurred retrieving the contact funnel report.");
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var pipeline = await _db.PipelineProjections.OrderBy(p => p.Stage).ToListAsync();
            var activities = await _db.ActivityRepProjections.OrderByDescending(a => a.TotalCount).ToListAsync();
            var contacts = await _db.ContactFunnelProjections.OrderBy(c => c.Status).ToListAsync();
            return Ok(new { pipeline, activities, contacts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard");
            return StatusCode(500, "An error occurred retrieving the dashboard.");
        }
    }
}
