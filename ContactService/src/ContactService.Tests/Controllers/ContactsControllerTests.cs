using ContactService.Controllers;
using ContactService.Models.DTOs;
using ContactService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharedLibrary.Contacts.Enums;

namespace ContactService.Tests.Controllers;

public class ContactsControllerTests
{
  private readonly Mock<IContactService> _serviceMock = new();
  private readonly Mock<ILogger<ContactsController>> _loggerMock = new();
  private readonly ContactsController _sut;

  public ContactsControllerTests()
  {
    _sut = new ContactsController(_serviceMock.Object, _loggerMock.Object);
  }

  // ── GetAll ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetAll_ReturnsOk_WithContactList()
  {
    var contacts = new List<ContactResponse>
    {
      new() { ContactId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" }
    };
    _serviceMock.Setup(s => s.GetAllContactsAsync(null, null, null))
      .ReturnsAsync(ServiceResult.Success(contacts));

    var result = await _sut.GetAll(null, null, null);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetAll_PassesFilters_ToService()
  {
    var ownerId = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetAllContactsAsync(ContactStatus.Lead, ownerId, null))
      .ReturnsAsync(ServiceResult.Success(new List<ContactResponse>()));

    var result = await _sut.GetAll(ContactStatus.Lead, ownerId, null);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
    _serviceMock.Verify(s => s.GetAllContactsAsync(ContactStatus.Lead, ownerId, null), Times.Once);
  }

  [Fact]
  public async Task GetAll_ReturnsError_WhenServiceFails()
  {
    // Covers the null Data ?? result.Message branch in GetAll
    _serviceMock.Setup(s => s.GetAllContactsAsync(null, null, null))
      .ReturnsAsync(ServiceResult.Failure("Service error.", 503));

    var result = await _sut.GetAll(null, null, null);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(503);
  }

  [Fact]
  public async Task GetAll_Returns500_OnException()
  {
    _serviceMock.Setup(s => s.GetAllContactsAsync(null, null, null))
      .ThrowsAsync(new Exception("db error"));

    var result = await _sut.GetAll(null, null, null);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── GetById ───────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetById_ReturnsOk_WhenFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetContactAsync(id))
      .ReturnsAsync(ServiceResult.Success(new ContactResponse { ContactId = id, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" }));

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetById_Returns404_WhenNotFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetContactAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Not found", 404));

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task GetById_Returns500_OnException()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetContactAsync(id)).ThrowsAsync(new Exception());

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── Create ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Create_ReturnsBadRequest_WhenFirstNameMissing()
  {
    var result = await _sut.Create(new CreateContactRequest
    {
      FirstName = "",
      LastName = "Doe",
      Email = "jane@example.com"
    });

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task Create_ReturnsBadRequest_WhenLastNameMissing()
  {
    var result = await _sut.Create(new CreateContactRequest
    {
      FirstName = "Jane",
      LastName = "",
      Email = "jane@example.com"
    });

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task Create_ReturnsBadRequest_WhenEmailMissing()
  {
    var result = await _sut.Create(new CreateContactRequest
    {
      FirstName = "Jane",
      LastName = "Doe",
      Email = ""
    });

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task Create_Returns201_OnSuccess()
  {
    var request = new CreateContactRequest
    {
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane@example.com"
    };
    _serviceMock.Setup(s => s.CreateContactAsync(request))
      .ReturnsAsync(ServiceResult.Success(new ContactResponse { ContactId = Guid.NewGuid() }, "Created", 201));

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(201);
  }

  [Fact]
  public async Task Create_Returns500_OnException()
  {
    var request = new CreateContactRequest { FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
    _serviceMock.Setup(s => s.CreateContactAsync(request)).ThrowsAsync(new Exception());

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── Update ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Update_ReturnsOk_OnSuccess()
  {
    var id = Guid.NewGuid();
    var request = new UpdateContactRequest { FirstName = "Janet" };
    _serviceMock.Setup(s => s.UpdateContactAsync(id, request))
      .ReturnsAsync(ServiceResult.Success(new ContactResponse { ContactId = id }));

    var result = await _sut.Update(id, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task Update_Returns404_WhenNotFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.UpdateContactAsync(id, It.IsAny<UpdateContactRequest>()))
      .ReturnsAsync(ServiceResult.Failure("Not found", 404));

    var result = await _sut.Update(id, new UpdateContactRequest());

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task Update_Returns500_OnException()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.UpdateContactAsync(id, It.IsAny<UpdateContactRequest>()))
        .ThrowsAsync(new Exception("db error"));

    var result = await _sut.Update(id, new UpdateContactRequest());

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Delete_ReturnsNoContent_OnSuccess()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteContactAsync(id))
      .ReturnsAsync(ServiceResult.Success());

    var result = await _sut.Delete(id);

    result.Should().BeOfType<NoContentResult>();
  }

  [Fact]
  public async Task Delete_Returns404_WhenNotFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteContactAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Not found", 404));

    var result = await _sut.Delete(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task Delete_Returns500_OnException()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteContactAsync(id)).ThrowsAsync(new Exception());

    var result = await _sut.Delete(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }
}
