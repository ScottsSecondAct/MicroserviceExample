using AccountService.Models.Enums;

namespace AccountService.Models.DTOs;

public class AccountResponse
{
  public Guid AccountId { get; set; }
  public string Name { get; set; } = string.Empty;
  public AccountIndustry? Industry { get; set; }
  public AccountSize? Size { get; set; }
  public string? Website { get; set; }
  public string? Street { get; set; }
  public string? City { get; set; }
  public string? State { get; set; }
  public string? PostalCode { get; set; }
  public string? Country { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime UpdatedAt { get; set; }
}
