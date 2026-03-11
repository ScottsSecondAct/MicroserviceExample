namespace ReportingService.Models;

public class ActivityRepProjection
{
    public Guid OwnerId { get; set; }
    public int TotalCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
