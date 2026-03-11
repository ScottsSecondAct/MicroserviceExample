using ActivityService.Controllers;
using ActivityService.Models.DTOs;
using ActivityService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Activities.Enums;

namespace ActivityService.Tests.Controllers;

public class ActivitiesControllerTests
{
  private readonly Mock<IActivityService> _mockService;
  private readonly Mock<ILogger<ActivitiesController>> _mockLogger;
  private readonly ActivitiesController _controller;

  public ActivitiesControllerTests()
  {
    _mockService = new Mock<IActivityService>();
    _mockLogger = new Mock<ILogger<ActivitiesController>>();
    _controller = new ActivitiesController(_mockService.Object, _mockLogger.Object);
  }

  private static ActivityResponse MakeResponse() => new()
  {
    ActivityId = Guid.NewGuid(),
    Type = ActivityType.Call,
    Subject = "Test Activity",
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
  };

  [Fact]
  public async Task GetAll_ReturnsOk_WithList()
  {
    var activities = new List<ActivityResponse> { MakeResponse() };
    _mockService.Setup(s => s.GetAllActivitiesAsync(null, null, null, null, null))
      .ReturnsAsync(ServiceResult.Success(activities));

    var result = await _controller.GetAll(null, null, null, null, null);

    var objectResult = result as ObjectResult;
    objectResult.Should().NotBeNull();
    objectResult!.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetById_WhenFound_ReturnsOk()
  {
    var response = MakeResponse();
    _mockService.Setup(s => s.GetActivityAsync(response.ActivityId))
      .ReturnsAsync(ServiceResult.Success(response));

    var result = await _controller.GetById(response.ActivityId);

    var objectResult = result as ObjectResult;
    objectResult!.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetById_WhenNotFound_Returns404()
  {
    var id = Guid.NewGuid();
    _mockService.Setup(s => s.GetActivityAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Activity not found.", 404));

    var result = await _controller.GetById(id);

    var objectResult = result as ObjectResult;
    objectResult!.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task Create_WithValidRequest_Returns201()
  {
    var response = MakeResponse();
    var request = new CreateActivityRequest { Type = ActivityType.Call, Subject = "Test" };
    _mockService.Setup(s => s.CreateActivityAsync(request))
      .ReturnsAsync(ServiceResult.Success(response, "Activity created successfully.", 201));

    var result = await _controller.Create(request);

    var objectResult = result as ObjectResult;
    objectResult!.StatusCode.Should().Be(201);
  }

  [Fact]
  public async Task Create_WithEmptySubject_Returns400()
  {
    var request = new CreateActivityRequest { Type = ActivityType.Call, Subject = "" };

    var result = await _controller.Create(request);

    var badRequest = result as BadRequestObjectResult;
    badRequest.Should().NotBeNull();
  }

  [Fact]
  public async Task Update_WhenFound_ReturnsOk()
  {
    var response = MakeResponse();
    var request = new UpdateActivityRequest { Subject = "Updated" };
    _mockService.Setup(s => s.UpdateActivityAsync(response.ActivityId, request))
      .ReturnsAsync(ServiceResult.Success(response));

    var result = await _controller.Update(response.ActivityId, request);

    var objectResult = result as ObjectResult;
    objectResult!.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task Delete_WhenFound_ReturnsNoContent()
  {
    var id = Guid.NewGuid();
    _mockService.Setup(s => s.DeleteActivityAsync(id))
      .ReturnsAsync(ServiceResult.Success(statusCode: 204));

    var result = await _controller.Delete(id);

    result.Should().BeOfType<NoContentResult>();
  }

  [Fact]
  public async Task Delete_WhenNotFound_Returns404()
  {
    var id = Guid.NewGuid();
    _mockService.Setup(s => s.DeleteActivityAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Activity not found.", 404));

    var result = await _controller.Delete(id);

    var objectResult = result as ObjectResult;
    objectResult!.StatusCode.Should().Be(404);
  }
}
