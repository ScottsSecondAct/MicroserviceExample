namespace ReportingService.Models;

// Tracks each deal's current stage and value so stage-change events can correctly
// update the pipeline projection without needing to repeat the value in every event.
public class DealSnapshot
{
    public Guid DealId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
