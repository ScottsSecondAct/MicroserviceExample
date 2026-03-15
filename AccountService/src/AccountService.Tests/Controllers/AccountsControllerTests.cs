using AccountService.Controllers;
using AccountService.Models.DTOs;
using AccountService.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.Controllers;

public class AccountsControllerTests
{
  private readonly Mock<IAccountService> _serviceMock = new();
  private readonly Mock<ILogger<AccountsController>> _loggerMock = new();
  private readonly AccountsController _sut;

  public AccountsControllerTests()
  {
    _sut = new AccountsController(_serviceMock.Object, _loggerMock.Object);
  }

  // ── GetAll ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetAll_ReturnsOk_WithAccountList()
  {
    var accounts = new List<AccountResponse>
    {
      new() { AccountId = Guid.NewGuid(), Name = "Acme" }
    };
    _serviceMock.Setup(s => s.GetAllAccountsAsync())
      .ReturnsAsync(ServiceResult.Success(accounts));

    var result = await _sut.GetAll();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetAll_ServiceFailure_ReturnsErrorMessage()
  {
    _serviceMock.Setup(s => s.GetAllAccountsAsync())
      .ReturnsAsync(ServiceResult.Failure("Service unavailable.", 503));

    var result = await _sut.GetAll();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(503);
  }

  [Fact]
  public async Task GetAll_Returns500_OnException()
  {
    _serviceMock.Setup(s => s.GetAllAccountsAsync()).ThrowsAsync(new Exception("db error"));

    var result = await _sut.GetAll();

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── GetById ───────────────────────────────────────────────────────────────

  [Fact]
  public async Task GetById_ReturnsOk_WhenFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetAccountAsync(id))
      .ReturnsAsync(ServiceResult.Success(new AccountResponse { AccountId = id, Name = "Acme" }));

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task GetById_Returns404_WhenNotFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetAccountAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Not found", 404));

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task GetById_Returns500_OnException()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.GetAccountAsync(id)).ThrowsAsync(new Exception());

    var result = await _sut.GetById(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── Create ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Create_ReturnsBadRequest_WhenNameMissing()
  {
    var result = await _sut.Create(new CreateAccountRequest { Name = "" });

    result.Should().BeOfType<BadRequestObjectResult>();
  }

  [Fact]
  public async Task Create_Returns201_OnSuccess()
  {
    var request = new CreateAccountRequest { Name = "Acme" };
    _serviceMock.Setup(s => s.CreateAccountAsync(request))
      .ReturnsAsync(ServiceResult.Success(new AccountResponse { AccountId = Guid.NewGuid(), Name = "Acme" }, "Created", 201));

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(201);
  }

  [Fact]
  public async Task Create_ServiceFailure_ReturnsErrorMessage()
  {
    var request = new CreateAccountRequest { Name = "Acme" };
    _serviceMock.Setup(s => s.CreateAccountAsync(request))
      .ReturnsAsync(ServiceResult.Failure("Conflict.", 409));

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(409);
  }

  [Fact]
  public async Task Create_Returns500_OnException()
  {
    var request = new CreateAccountRequest { Name = "Acme" };
    _serviceMock.Setup(s => s.CreateAccountAsync(request)).ThrowsAsync(new Exception());

    var result = await _sut.Create(request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── Update ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Update_ReturnsOk_OnSuccess()
  {
    var id = Guid.NewGuid();
    var request = new UpdateAccountRequest { Name = "Updated" };
    _serviceMock.Setup(s => s.UpdateAccountAsync(id, request))
      .ReturnsAsync(ServiceResult.Success(new AccountResponse { AccountId = id, Name = "Updated" }));

    var result = await _sut.Update(id, request);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(200);
  }

  [Fact]
  public async Task Update_Returns404_WhenNotFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.UpdateAccountAsync(id, It.IsAny<UpdateAccountRequest>()))
      .ReturnsAsync(ServiceResult.Failure("Not found", 404));

    var result = await _sut.Update(id, new UpdateAccountRequest());

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task Update_Returns500_OnException()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.UpdateAccountAsync(id, It.IsAny<UpdateAccountRequest>()))
      .ThrowsAsync(new Exception());

    var result = await _sut.Update(id, new UpdateAccountRequest());

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }

  // ── Delete ────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Delete_ReturnsNoContent_OnSuccess()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteAccountAsync(id))
      .ReturnsAsync(ServiceResult.Success());

    var result = await _sut.Delete(id);

    result.Should().BeOfType<NoContentResult>();
  }

  [Fact]
  public async Task Delete_Returns404_WhenNotFound()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteAccountAsync(id))
      .ReturnsAsync(ServiceResult.Failure("Not found", 404));

    var result = await _sut.Delete(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task Delete_Returns500_OnException()
  {
    var id = Guid.NewGuid();
    _serviceMock.Setup(s => s.DeleteAccountAsync(id)).ThrowsAsync(new Exception());

    var result = await _sut.Delete(id);

    var obj = result.Should().BeOfType<ObjectResult>().Subject;
    obj.StatusCode.Should().Be(500);
  }
}
