using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using AuthService.Controllers;
using AuthService.Services;
using AuthService.Models.DTOs;

public class RegistrationControllerTests
{
  private readonly Mock<IRegistrationService> _mockRegistrationService;
  private readonly Mock<ILogger<RegistrationController>> _mockLogger;
  private readonly RegistrationController _controller;

  public RegistrationControllerTests()
  {
    _mockRegistrationService = new Mock<IRegistrationService>();
    _mockLogger = new Mock<ILogger<RegistrationController>>();
    _controller = new RegistrationController(_mockRegistrationService.Object, _mockLogger.Object);
  }

  [Fact]
  public async Task Register_ShouldReturnOk_WhenRegistrationSucceeds()
  {
    // Arrange
    var request = new RegisterRequest { Email = "test@example.com", Password = "SecurePassword123" };
    _mockRegistrationService
        .Setup(service => service.RegisterUserAsync(request.Email, request.Password))
        .ReturnsAsync(ServiceResult.Success(null, "User registered successfully."));

    // Act
    var result = await _controller.Register(request);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    Assert.NotNull(okResult.Value);
    Assert.Contains("User registered successfully.", okResult.Value.ToString());
  }

  [Fact]
  public async Task Register_ShouldReturnBadRequest_WhenEmailOrPasswordIsMissing()
  {
    // Arrange
    var request = new RegisterRequest { Email = "", Password = "" };

    // Act
    var result = await _controller.Register(request);

    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    Assert.NotNull(badRequestResult.Value);
    Assert.Contains("Email and password are required.", badRequestResult.Value.ToString());
  }

  [Fact]
  public async Task Register_ShouldReturnStatusCode_WhenServiceFails()
  {
    // Arrange
    var request = new RegisterRequest { Email = "test@example.com", Password = "SecurePassword123" };
    _mockRegistrationService
        .Setup(service => service.RegisterUserAsync(request.Email, request.Password))
        .ReturnsAsync(ServiceResult.Failure("Email is already registered.", 409));

    // Act
    var result = await _controller.Register(request);

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(409, statusCodeResult.StatusCode);
    Assert.NotNull(statusCodeResult.Value);
    Assert.Contains("Email is already registered.", statusCodeResult.Value.ToString());
  }

  [Fact]
  public async Task Register_ShouldReturnInternalServerError_WhenExceptionIsThrown()
  {
    // Arrange
    var request = new RegisterRequest { Email = "test@example.com", Password = "SecurePassword123" };
    _mockRegistrationService
        .Setup(service => service.RegisterUserAsync(request.Email, request.Password))
        .ThrowsAsync(new Exception("Database error"));

    // Act
    var result = await _controller.Register(request);

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);
    Assert.NotNull(statusCodeResult.Value);
    Assert.Contains("An internal server error occurred.", statusCodeResult.Value.ToString());

    // Verify logger call
    _mockLogger.Verify(
        logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, t) => state.ToString().Contains("An error occurred while processing the registration.")),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((state, ex) => true)),
        Times.Once);
  }
}
