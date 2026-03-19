using DealService.Models;
using DealService.Models.DTOs;
using DealService.Repository;
using DealService.Services;
using FluentAssertions;
using MassTransit;
using Moq;
using SharedLibrary.Deals.Enums;
using SharedLibrary.Deals.Events;

namespace DealService.Tests.Services;

public class DealsServiceTests
{
  private readonly Mock<IDealRepository> _mockRepository;
  private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
  private readonly Mock<IAccountClient> _mockAccountClient;
  private readonly Mock<IContactClient> _mockContactClient;
  private readonly DealsService _service;

  public DealsServiceTests()
  {
    _mockRepository = new Mock<IDealRepository>();
    _mockPublishEndpoint = new Mock<IPublishEndpoint>();
    _mockAccountClient = new Mock<IAccountClient>();
    _mockContactClient = new Mock<IContactClient>();
    _service = new DealsService(
      _mockRepository.Object,
      _mockPublishEndpoint.Object,
      _mockAccountClient.Object,
      _mockContactClient.Object);
  }

  private static Deal MakeDeal(DealStage stage = DealStage.Prospecting) => new()
  {
    DealId = Guid.NewGuid(),
    Title = "Test Deal",
    Stage = stage,
    Value = 10000m,
    Probability = 50,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    DealContacts = new List<DealContact>()
  };

  // ── CreateDealAsync ────────────────────────────────────────────────────────

  [Fact]
  public async Task CreateDealAsync_WithValidRequest_ReturnsSuccess()
  {
    var request = new CreateDealRequest
    {
      Title = "New Deal",
      Stage = DealStage.Prospecting,
      Value = 5000m,
      Probability = 30
    };

    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<DealCreated>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.CreateDealAsync(request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    var response = result.Data as DealResponse;
    response.Should().NotBeNull();
    response!.Title.Should().Be("New Deal");
    response.Stage.Should().Be(DealStage.Prospecting);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockAccountClient.Verify(a => a.AccountExistsAsync(It.IsAny<Guid>()), Times.Never);
    _mockContactClient.Verify(c => c.ContactExistsAsync(It.IsAny<Guid>()), Times.Never);
  }

  [Fact]
  public async Task CreateDealAsync_WithValidAccountId_ValidatesAndCreates()
  {
    var accountId = Guid.NewGuid();
    var request = new CreateDealRequest
    {
      Title = "New Deal",
      AccountId = accountId,
      Value = 5000m
    };

    _mockAccountClient.Setup(a => a.AccountExistsAsync(accountId)).ReturnsAsync(true);
    _mockRepository.Setup(r => r.AddAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<DealCreated>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.CreateDealAsync(request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    _mockAccountClient.Verify(a => a.AccountExistsAsync(accountId), Times.Once);
  }

  [Fact]
  public async Task CreateDealAsync_WithInvalidAccountId_ReturnsFailure()
  {
    var accountId = Guid.NewGuid();
    var request = new CreateDealRequest
    {
      Title = "New Deal",
      AccountId = accountId,
      Value = 5000m
    };

    _mockAccountClient.Setup(a => a.AccountExistsAsync(accountId)).ReturnsAsync(false);

    var result = await _service.CreateDealAsync(request);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
    _mockRepository.Verify(r => r.AddAsync(It.IsAny<Deal>()), Times.Never);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealCreated>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  // ── GetDealAsync ───────────────────────────────────────────────────────────

  [Fact]
  public async Task GetDealAsync_WhenFound_ReturnsSuccess()
  {
    var deal = MakeDeal();
    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);

    var result = await _service.GetDealAsync(deal.DealId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as DealResponse;
    response.Should().NotBeNull();
    response!.DealId.Should().Be(deal.DealId);
  }

  [Fact]
  public async Task GetDealAsync_WhenNotFound_ReturnsFailure()
  {
    var id = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Deal?)null);

    var result = await _service.GetDealAsync(id);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  // ── GetAllDealsAsync ───────────────────────────────────────────────────────

  [Fact]
  public async Task GetAllDealsAsync_ReturnsSuccess()
  {
    var deals = new List<Deal> { MakeDeal(), MakeDeal() };
    _mockRepository.Setup(r => r.GetAllAsync(null, null, null)).ReturnsAsync(deals);

    var result = await _service.GetAllDealsAsync();

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    var response = result.Data as List<DealResponse>;
    response.Should().NotBeNull();
    response!.Count.Should().Be(2);
  }

  // ── UpdateDealAsync ────────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateDealAsync_WhenStageChanges_PublishesDealStageChanged()
  {
    var deal = MakeDeal(DealStage.Prospecting);
    var request = new UpdateDealRequest { Stage = DealStage.Proposal };

    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.UpdateDealAsync(deal.DealId, request);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(200);
    _mockPublishEndpoint.Verify(
      p => p.Publish(
        It.Is<DealStageChanged>(e =>
          e.DealId == deal.DealId &&
          e.OldStage == DealStage.Prospecting &&
          e.NewStage == DealStage.Proposal),
        It.IsAny<CancellationToken>()),
      Times.Once);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealClosed>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task UpdateDealAsync_WhenStageChangesToClosedWon_PublishesBothEvents()
  {
    var deal = MakeDeal(DealStage.Negotiation);
    var request = new UpdateDealRequest { Stage = DealStage.ClosedWon };

    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<DealClosed>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.UpdateDealAsync(deal.DealId, request);

    result.IsSuccess.Should().BeTrue();
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockPublishEndpoint.Verify(
      p => p.Publish(
        It.Is<DealClosed>(e => e.DealId == deal.DealId && e.Stage == DealStage.ClosedWon),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task UpdateDealAsync_WhenStageChangesToClosedLost_PublishesBothEvents()
  {
    var deal = MakeDeal(DealStage.Negotiation);
    var request = new UpdateDealRequest { Stage = DealStage.ClosedLost };

    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    _mockPublishEndpoint
      .Setup(p => p.Publish(It.IsAny<DealClosed>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var result = await _service.UpdateDealAsync(deal.DealId, request);

    result.IsSuccess.Should().BeTrue();
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()), Times.Once);
    _mockPublishEndpoint.Verify(
      p => p.Publish(
        It.Is<DealClosed>(e => e.DealId == deal.DealId && e.Stage == DealStage.ClosedLost),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task UpdateDealAsync_WhenStageUnchanged_DoesNotPublishStageChanged()
  {
    var deal = MakeDeal(DealStage.Prospecting);
    var request = new UpdateDealRequest { Title = "Updated Title", Stage = DealStage.Prospecting };

    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateDealAsync(deal.DealId, request);

    result.IsSuccess.Should().BeTrue();
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()),
      Times.Never);
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealClosed>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task UpdateDealAsync_WhenNotFound_ReturnsFailure()
  {
    var id = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Deal?)null);

    var result = await _service.UpdateDealAsync(id, new UpdateDealRequest());

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
  }

  // ── DeleteDealAsync ────────────────────────────────────────────────────────

  [Fact]
  public async Task UpdateDealAsync_WhenAllOptionalFieldsProvided_UpdatesAll()
  {
    var deal = MakeDeal(DealStage.Prospecting);
    var accountId = Guid.NewGuid();
    var ownerId = Guid.NewGuid();
    var closeDate = DateTime.UtcNow.AddDays(30);
    var request = new UpdateDealRequest
    {
      AccountId = accountId,
      Value = 99000m,
      Probability = 75,
      ExpectedCloseDate = closeDate,
      OwnerId = ownerId
    };

    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateDealAsync(deal.DealId, request);

    result.IsSuccess.Should().BeTrue();
    var response = result.Data as DealResponse;
    response!.AccountId.Should().Be(accountId);
    response.Value.Should().Be(99000m);
    response.Probability.Should().Be(75);
    response.OwnerId.Should().Be(ownerId);
    // No stage change → no events published
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task UpdateDealAsync_WhenNoStageProvided_DoesNotPublishEvent()
  {
    var deal = MakeDeal(DealStage.Prospecting);
    var request = new UpdateDealRequest { Title = "Title Only" };

    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Deal>())).Returns(Task.CompletedTask);

    var result = await _service.UpdateDealAsync(deal.DealId, request);

    result.IsSuccess.Should().BeTrue();
    _mockPublishEndpoint.Verify(
      p => p.Publish(It.IsAny<DealStageChanged>(), It.IsAny<CancellationToken>()), Times.Never);
  }

  [Fact]
  public async Task CreateDealAsync_WithEmptyTitle_ReturnsFailure()
  {
    var request = new CreateDealRequest { Title = "" };

    var result = await _service.CreateDealAsync(request);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
    _mockRepository.Verify(r => r.AddAsync(It.IsAny<Deal>()), Times.Never);
  }

  // ── AddContactToDealAsync ──────────────────────────────────────────────────

  [Fact]
  public async Task AddContactToDealAsync_WhenDealNotFound_ReturnsFailure404()
  {
    var dealId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(dealId)).ReturnsAsync((Deal?)null);

    var result = await _service.AddContactToDealAsync(dealId, new AddDealContactRequest { ContactId = Guid.NewGuid() });

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
    _mockContactClient.Verify(c => c.ContactExistsAsync(It.IsAny<Guid>()), Times.Never);
  }

  [Fact]
  public async Task AddContactToDealAsync_WhenContactNotFound_ReturnsFailure400()
  {
    var deal = MakeDeal();
    var contactId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockContactClient.Setup(c => c.ContactExistsAsync(contactId)).ReturnsAsync(false);

    var result = await _service.AddContactToDealAsync(deal.DealId, new AddDealContactRequest { ContactId = contactId });

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(400);
    _mockRepository.Verify(r => r.AddDealContactAsync(It.IsAny<DealContact>()), Times.Never);
  }

  [Fact]
  public async Task AddContactToDealAsync_WhenAlreadyAssociated_ReturnsConflict409()
  {
    var deal = MakeDeal();
    var contactId = Guid.NewGuid();
    var existing = new DealContact { DealContactId = Guid.NewGuid(), DealId = deal.DealId, ContactId = contactId };
    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockContactClient.Setup(c => c.ContactExistsAsync(contactId)).ReturnsAsync(true);
    _mockRepository.Setup(r => r.GetDealContactAsync(deal.DealId, contactId)).ReturnsAsync(existing);

    var result = await _service.AddContactToDealAsync(deal.DealId, new AddDealContactRequest { ContactId = contactId });

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(409);
    _mockRepository.Verify(r => r.AddDealContactAsync(It.IsAny<DealContact>()), Times.Never);
  }

  [Fact]
  public async Task AddContactToDealAsync_Success_ReturnsCreated201()
  {
    var deal = MakeDeal();
    var contactId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockContactClient.Setup(c => c.ContactExistsAsync(contactId)).ReturnsAsync(true);
    _mockRepository.Setup(r => r.GetDealContactAsync(deal.DealId, contactId)).ReturnsAsync((DealContact?)null);
    _mockRepository.Setup(r => r.AddDealContactAsync(It.IsAny<DealContact>())).Returns(Task.CompletedTask);

    var result = await _service.AddContactToDealAsync(deal.DealId, new AddDealContactRequest
    {
      ContactId = contactId,
      Role = DealContactRole.DecisionMaker
    });

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(201);
    var response = result.Data as DealContactResponse;
    response!.ContactId.Should().Be(contactId);
    response.Role.Should().Be(DealContactRole.DecisionMaker);
    _mockRepository.Verify(r => r.AddDealContactAsync(It.IsAny<DealContact>()), Times.Once);
  }

  // ── RemoveContactFromDealAsync ─────────────────────────────────────────────

  [Fact]
  public async Task RemoveContactFromDealAsync_WhenNotAssociated_ReturnsFailure404()
  {
    var dealId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetDealContactAsync(dealId, contactId)).ReturnsAsync((DealContact?)null);

    var result = await _service.RemoveContactFromDealAsync(dealId, contactId);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
    _mockRepository.Verify(r => r.RemoveDealContactAsync(It.IsAny<Guid>()), Times.Never);
  }

  [Fact]
  public async Task RemoveContactFromDealAsync_Success_Returns204()
  {
    var dealId = Guid.NewGuid();
    var contactId = Guid.NewGuid();
    var existing = new DealContact { DealContactId = Guid.NewGuid(), DealId = dealId, ContactId = contactId };
    _mockRepository.Setup(r => r.GetDealContactAsync(dealId, contactId)).ReturnsAsync(existing);
    _mockRepository.Setup(r => r.RemoveDealContactAsync(existing.DealContactId)).Returns(Task.CompletedTask);

    var result = await _service.RemoveContactFromDealAsync(dealId, contactId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(204);
    _mockRepository.Verify(r => r.RemoveDealContactAsync(existing.DealContactId), Times.Once);
  }

  [Fact]
  public async Task DeleteDealAsync_WhenFound_ReturnsSuccess()
  {
    var deal = MakeDeal();
    _mockRepository.Setup(r => r.GetByIdAsync(deal.DealId)).ReturnsAsync(deal);
    _mockRepository.Setup(r => r.DeleteAsync(deal.DealId)).Returns(Task.CompletedTask);

    var result = await _service.DeleteDealAsync(deal.DealId);

    result.IsSuccess.Should().BeTrue();
    result.StatusCode.Should().Be(204);
    _mockRepository.Verify(r => r.DeleteAsync(deal.DealId), Times.Once);
  }

  [Fact]
  public async Task DeleteDealAsync_WhenNotFound_ReturnsFailure()
  {
    var id = Guid.NewGuid();
    _mockRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Deal?)null);

    var result = await _service.DeleteDealAsync(id);

    result.IsSuccess.Should().BeFalse();
    result.StatusCode.Should().Be(404);
    _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
  }
}
