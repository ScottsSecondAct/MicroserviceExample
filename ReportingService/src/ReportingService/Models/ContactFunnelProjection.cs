namespace ReportingService.Models;

public class ContactFunnelProjection
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
