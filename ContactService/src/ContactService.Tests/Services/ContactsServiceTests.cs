using ContactService.Models;
using ContactService.Models.DTOs;
using ContactService.Repository;
using ContactService.Services;
using FluentAssertions;
using MassTransit;
using Moq;
using SharedLibrary.Contacts.Enums;
using SharedLibrary.Contacts.Events;

namespace ContactService.Tests.Services;

public class ContactsServiceTests
{
  private readonly Mock<IContactRepository> _mockRepository;
  private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
  private readonly Mock<IAccountClient> _mockAccountClient;
  private readonly ContactsService _service;

  public ContactsServiceTests()
  {
    _mockRepository = new Mock<IContactRepository>();
    _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    _mockAccountClient = new Mock<IAccountClient>();
    _service = new ContactsService(
      _mockRepository.Object,
      _mockPublishEndpoint.Object,
      _mockAccountClient.Object);
  }

  [Fact]
  public async Task CreateContactAsync_WithValidRequest_ReturnsSuccess()
  {
    var request = new CreateContactRequest
    {
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane.doe@example.com",
      Status = ContactStatus.Lead
    };

    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Contact>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<ContactCreated>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.CreateContactAsync(request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    var response = result.Data as ContactResponse;
    response.Should().NotBeNull();
    response!.FirstName.Should().Be("Jane");
    response.LastName.Should().Be("Doe");
    response.Email.Should().Be("jane.doe@example.com");
    response.Status.Should().Be(ContactStatus.Lead);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<ContactCreated>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task CreateContactAsync_WithValidAccountId_ValidatesAndCreates()
  {
    var accountId = Guid.NewGuid();
    var request = new CreateContactRequest
    {
      FirstName = "John",
      LastName = "Smith",
      Email = "john.smith@example.com",
      AccountId = accountId
    };

    _mockAccountClient.Setup(a => a.AccountExistsAsync(accountId)).ReturnsAsync(true);
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Contact>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<ContactCreated>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.CreateContactAsync(request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    _mockAccountClient.Verify(a => a.AccountExistsAsync(accountId), Times.Once);
  }

  [Fact]
  public async Task CreateContactAsync_WithInvalidAccountId_ReturnsFailure()
  {
    var accountId = Guid.NewGuid();
    var request = new CreateContactRequest
    {
      FirstName = "John",
      LastName = "Smith",
      Email = "john.smith@example.com",
      AccountId = accountId
    };

    _mockAccountClient.Setup(a => a.AccountExistsAsync(accountId)).ReturnsAsync(false);

    var result = await _service.CreateContactAsync(request);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
    _mockRepository.Verify(r => r.AddAsync(It.IsAny<Contact>()), Times.Never);
  }

  [Fact]
  public async Task GetContactAsync_WhenFound_ReturnsSuccess()
  {
    var contactId = Guid.NewGuid();
    var contact = new Contact
    {
      ContactId = contactId,
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane@example.com",
      Status = ContactStatus.Lead,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _mockRepository.Setup(r => r.GetByIdAsync(contactId)).ReturnsAsync(contact);

    var result = await _service.GetContactAsync(contactId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as ContactResponse;
    response.Should().NotBeNull();
    response!.ContactId.Should().Be(contactId);
  }

  [Fact]
  public async Task GetContactAsync_WhenNotFound_ReturnsFailure()
  {
    var contactId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(contactId)).ReturnsAsync((Contact?)null);

    var result = await _service.GetContactAsync(contactId);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task UpdateContactAsync_WhenStatusChanges_PublishesContactStatusChanged()
  {
    var contactId = Guid.NewGuid();
    var contact = new Contact
    {
      ContactId = contactId,
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane@example.com",
      Status = ContactStatus.Lead,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
    var request = new UpdateContactRequest { Status = ContactStatus.Customer };

    _mockRepository.Setup(r => r.GetByIdAsync(contactId)).ReturnsAsync(contact);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Contact>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<ContactStatusChanged>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.UpdateContactAsync(contactId, request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    _mockPublishEndpoint.Verify(
      p => p.Publish(
        It.Is<ContactStatusChanged>(e =>
          e.ContactId == contactId &&
          e.OldStatus == ContactStatus.Lead &&
          e.NewStatus == ContactStatus.Customer),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task UpdateContactAsync_WhenStatusUnchanged_DoesNotPublishStatusChanged()
  {
    var contactId = Guid.NewGuid();
    var contact = new Contact
    {
      ContactId = contactId,
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane@example.com",
      Status = ContactStatus.Lead,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
    var request = new UpdateContactRequest { FirstName = "Janet", Status = ContactStatus.Lead };

    _mockRepository.Setup(r => r.GetByIdAsync(contactId)).ReturnsAsync(contact);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Contact>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateContactAsync(contactId, request);

    result.IsSuccess.Should().BeTrue();
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<ContactStatusChanged>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task UpdateContactAsync_WhenNotFound_ReturnsFailure()
  {
    var contactId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(contactId)).ReturnsAsync((Contact?)null);

    var result = await _service.UpdateContactAsync(contactId, new UpdateContactRequest());

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  [Fact]
  public async Task DeleteContactAsync_WhenFound_ReturnsSuccess()
  {
    var contactId = Guid.NewGuid();
    var contact = new Contact
    {
      ContactId = contactId,
      FirstName = "Jane",
      LastName = "Doe",
      Email = "jane@example.com",
      Status = ContactStatus.Lead,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    _mockRepository.Setup(r => r.GetByIdAsync(contactId)).ReturnsAsync(contact);
    _mockRepository.Setup(r => r.DeleteAsync(contactId)).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<ContactDeleted>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.DeleteContactAsync(contactId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(204);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<ContactDeleted>(), It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task DeleteContactAsync_WhenNotFound_ReturnsFailure()
  {
    var contactId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(contactId)).ReturnsAsync((Contact?)null);

    var result = await _service.DeleteContactAsync(contactId);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }
}
