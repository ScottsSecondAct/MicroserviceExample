using AuthService.Services;
using FluentAssertions;

namespace AuthService.Tests.Services;

public class ServiceResultTests
{
  [Fact]
  public void Success_WithDefaults_ReturnsSuccessResult()
  {
    var result = ServiceResult.Success();

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    result.Message.Should().Be("Success");
    result.Data.Should().BeNull();
  }

  [Fact]
  public void Success_WithAllArgs_ReturnsPopulatedResult()
  {
    var data = new { id = 1 };
    var result = ServiceResult.Success(data, "Created", 201);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    result.Message.Should().Be("Created");
    result.Data.Should().Be(data);
  }

  [Fact]
  public void Failure_WithDefaults_ReturnsFailureResult()
  {
    var result = ServiceResult.Failure("Bad request");

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
    result.Message.Should().Be("Bad request");
  }

  [Fact]
  public void Failure_WithCustomStatusCode_Returns404()
  {
    var result = ServiceResult.Failure("Not found", 404);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public void Error_WithDefaults_ReturnsFailureWith500()
  {
    var result = ServiceResult.Error("Server error");

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(500);
    result.Message.Should().Be("Server error");
  }

  [Fact]
  public void Error_WithCustomStatusCode_Returns503()
  {
    var result = ServiceResult.Error("Unavailable", 503);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(503);
  }
}
