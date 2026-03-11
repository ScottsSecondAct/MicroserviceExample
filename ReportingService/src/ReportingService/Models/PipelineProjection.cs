namespace ReportingService.Models;

public class PipelineProjection
{
    public string Stage { get; set; } = string.Empty;
    public int DealCount { get; set; }
    public decimal TotalValue { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
