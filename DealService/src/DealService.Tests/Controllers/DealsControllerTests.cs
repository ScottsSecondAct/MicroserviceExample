using DealService.Controllers;
using DealService.Models.DTOs;
using DealService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Deals.Enums;

namespace DealService.Tests.Controllers;

public class DealsControllerTests
{
  private readonly Mock<IDealService> _serviceMock = new();
  private readonly Mock<ILogger<DealsController>> _loggerMock = new();
  private readonly DealsController _sut;

  public DealsControllerTests()
  {
    _sut = new DealsController(_serviceMock.Object, _loggerMock.Object);
  }

  // ── GetAll ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetAll_ReturnsOk()
  {
    var deals = new List<DealResponse>
    {
      new() { DealId = Guid.NewGuid(), Title = "Deal A" }
    };
    _serviceMock.Setup(s => s.GetAllDealsAsync(null, null, null))
      .ReturnsAsync(ServiceResult.Success(deals));

    var result = await _sut.GetAll(null, null, null);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetAll_WithFilters_PassesFiltersToService()
  {
    var ownerId = Guid.NewGuid();
    var accountId = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetAllDealsAsync(DealStage.Prospecting, accountId, ownerId))
      .ReturnsAsync(ServiceResult.Success(new List<DealResponse>()));

    var result = await _sut.GetAll(DealStage.Prospecting, accountId, ownerId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
    _serviceMock.Verify(s => s.GetAllDealsAsync(DealStage.Prospecting, accountId, ownerId), Times.Once);
  }

  // ── GetById ───────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetById_WhenFound_ReturnsOk()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetDealAsync(id))
      .ReturnsAsync(ServiceResult.Success(new DealResponse { DealId = id, Title = "Deal A" }));

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetById_WhenNotFound_Returns404()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetDealAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Deal not found.", 404));

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  // ── Create ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Create_WithValidRequest_Returns201()
  {
    var request = new CreateDealRequest { Title = "New Deal", Value = 5000m };
    _serviceMock.Setup(s => s.CreateDealAsync(request))
      .ReturnsAsync(ServiceResult.Success(new DealResponse { DealId = Guid.NewGuid(), Title = "New Deal" }, "Created", 201));

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(201);
  }

  [Fact]
  public async Task Create_WithEmptyTitle_Returns400()
  {
    var result = await _sut.Create(new CreateDealRequest { Title = "" });

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task Create_ServiceFailure_Returns400()
  {
    var request = new CreateDealRequest { Title = "New Deal" };
    _serviceMock.Setup(s => s.CreateDealAsync(request))
      .ReturnsAsync(ServiceResult.Failure("Account not found.", 400));

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(400);
  }

  // ── Update ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Update_WhenFound_ReturnsOk()
  {
    var id = Guid.NewGuid();
    var request = new UpdateDealRequest { Title = "Updated Deal" };
    _serviceMock.Setup(s => s.UpdateDealAsync(id, request))
      .ReturnsAsync(ServiceResult.Success(new DealResponse { DealId = id, Title = "Updated Deal" }));

    var result = await _sut.Update(id, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task Update_WhenNotFound_Returns404()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.UpdateDealAsync(id, It.IsAny<UpdateDealRequest>()))
      .ReturnsAsync(ServiceResult.Failure("Deal not found.", 404));

    var result = await _sut.Update(id, new UpdateDealRequest());

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Delete_WhenFound_Returns204()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteDealAsync(id))
      .ReturnsAsync(ServiceResult.Success(statusCode: 204));

    var result = await _sut.Delete(id);

    result.Should().BeOfType<NoContentResult>();
  }

  [Fact]
  public async Task Delete_WhenNotFound_Returns404()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteDealAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Deal not found.", 404));

    var result = await _sut.Delete(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  // ── AddContact ────────────────────────────────────────────────────────────

  [Fact]
  public async Task AddContact_WhenDealFound_Returns201()
  {
    var dealId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    var request = new AddDealContactRequest { ContactId = contactId, Role = DealContactRole.DecisionMaker };
    _serviceMock.Setup(s => s.AddContactToDealAsync(dealId, request))
      .ReturnsAsync(ServiceResult.Success(
        new DealContactResponse { ContactId = contactId, Role = DealContactRole.DecisionMaker },
        statusCode: 201));

    var result = await _sut.AddContact(dealId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(201);
  }

  // ── RemoveContact ─────────────────────────────────────────────────────────

  [Fact]
  public async Task RemoveContact_WhenFound_Returns204()
  {
    var dealId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    _serviceMock.Setup(s => s.RemoveContactFromDealAsync(dealId, contactId))
      .ReturnsAsync(ServiceResult.Success(statusCode: 204));

    var result = await _sut.RemoveContact(dealId, contactId);

    result.Should().BeOfType<NoContentResult>();
  }

  [Fact]
  public async Task RemoveContact_WhenNotFound_Returns404()
  {
    var dealId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    _serviceMock.Setup(s => s.RemoveContactFromDealAsync(dealId, contactId))
      .ReturnsAsync(ServiceResult.Failure("Contact is not associated with this deal.", 404));

    var result = await _sut.RemoveContact(dealId, contactId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  // ── Exception paths ───────────────────────────────────────────────────────

  [Fact]
  public async Task GetAll_ServiceThrows_Returns500()
  {
    _serviceMock.Setup(s => s.GetAllDealsAsync(null, null, null))
      .ThrowsAsync(new Exception("boom"));

    var result = await _sut.GetAll(null, null, null);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task GetById_ServiceThrows_Returns500()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetDealAsync(id)).ThrowsAsync(new Exception("boom"));

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task Create_ServiceThrows_Returns500()
  {
    var request = new CreateDealRequest { Title = "New Deal" };
    _serviceMock.Setup(s => s.CreateDealAsync(request)).ThrowsAsync(new Exception("boom"));

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task Update_ServiceThrows_Returns500()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.UpdateDealAsync(id, It.IsAny<UpdateDealRequest>()))
      .ThrowsAsync(new Exception("boom"));

    var result = await _sut.Update(id, new UpdateDealRequest());

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task Delete_ServiceThrows_Returns500()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteDealAsync(id)).ThrowsAsync(new Exception("boom"));

    var result = await _sut.Delete(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task AddContact_ServiceThrows_Returns500()
  {
    var dealId = Guid.NewGuid();
    var request = new AddDealContactRequest { ContactId = Guid.NewGuid(), Role = DealContactRole.DecisionMaker };
    _serviceMock.Setup(s => s.AddContactToDealAsync(dealId, request)).ThrowsAsync(new Exception("boom"));

    var result = await _sut.AddContact(dealId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  [Fact]
  public async Task RemoveContact_ServiceThrows_Returns500()
  {
    var dealId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    _serviceMock.Setup(s => s.RemoveContactFromDealAsync(dealId, contactId))
      .ThrowsAsync(new Exception("boom"));

    var result = await _sut.RemoveContact(dealId, contactId);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── Null-coalescing branch coverage ───────────────────────────────────────

  [Fact]
  public async Task GetAll_WithServiceFailure_ReturnsErrorMessage()
  {
    // Covers the null Data ?? result.Message branch in GetAll
    _serviceMock.Setup(s => s.GetAllDealsAsync(null, null, null))
      .ReturnsAsync(ServiceResult.Failure("Service unavailable.", 503));

    var result = await _sut.GetAll(null, null, null);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(503);
  }

  [Fact]
  public async Task AddContact_WithServiceFailure_ReturnsErrorCode()
  {
    // Covers the null Data ?? result.Message branch in AddContact
    var dealId = Guid.NewGuid();
    var request = new AddDealContactRequest { ContactId = Guid.NewGuid(), Role = DealContactRole.DecisionMaker };
    _serviceMock.Setup(s => s.AddContactToDealAsync(dealId, request))
      .ReturnsAsync(ServiceResult.Failure("Deal not found.", 404));

    var result = await _sut.AddContact(dealId, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }
}
