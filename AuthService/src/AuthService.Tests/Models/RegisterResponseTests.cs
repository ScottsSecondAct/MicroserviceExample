using AuthService.Models.DTOs;
using FluentAssertions;

namespace AuthService.Tests.Models;

public class RegisterResponseTests
{
  [Fact]
  public void RegisterResponse_PropertiesRoundtrip()
  {
    var id = Guid.NewGuid();
    var response = new RegisterResponse { UserId = id, Message = "Registered successfully." };

    response.UserId.Should().Be(id);
    response.Message.Should().Be("Registered successfully.");
  }

  [Fact]
  public void RegisterResponse_DefaultValues_AreExpected()
  {
    var response = new RegisterResponse();

    response.UserId.Should().Be(Guid.Empty);
    response.Message.Should().Be(string.Empty);
  }
}
