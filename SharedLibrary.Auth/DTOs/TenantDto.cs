namespace SharedLibrary.DTOs;

public class TenantDto
{
  public Guid TenantId { get; set; }
  public string Slug { get; set; } = string.Empty;
  public string DisplayName { get; set; } = string.Empty;
}
