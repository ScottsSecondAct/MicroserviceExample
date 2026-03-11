using DealService.Models;
using DealService.Models.DTOs;
using DealService.Repository;
using MassTransit;
using SharedLibrary.Deals.Enums;
using SharedLibrary.Deals.Events;

namespace DealService.Services;

public class DealsService : IDealService
{
  private readonly IDealRepository _repository;
  private readonly IPublishEndpoint _publishEndpoint;
  private readonly IAccountClient _accountClient;
  private readonly IContactClient _contactClient;

  public DealsService(
    IDealRepository repository,
    IPublishEndpoint publishEndpoint,
    IAccountClient accountClient,
    IContactClient contactClient)
  {
    _repository = repository;
    _publishEndpoint = publishEndpoint;
    _accountClient = accountClient;
    _contactClient = contactClient;
  }

  public async Task<ServiceResult> GetAllDealsAsync(DealStage? stage = null, Guid? accountId = null, Guid? ownerId = null)
  {
    var deals = await _repository.GetAllAsync(stage, accountId, ownerId);
    return ServiceResult.Success(deals.Select(MapToResponse).ToList());
  }

  public async Task<ServiceResult> GetDealAsync(Guid id)
  {
    var deal = await _repository.GetByIdAsync(id);
    if (deal == null)
      return ServiceResult.Failure("Deal not found.", 404);
    return ServiceResult.Success(MapToResponse(deal));
  }

  public async Task<ServiceResult> CreateDealAsync(CreateDealRequest request)
  {
    if (string.IsNullOrWhiteSpace(request.Title))
      return ServiceResult.Failure("Title is required.", 400);

    if (request.AccountId.HasValue)
    {
      var accountExists = await _accountClient.AccountExistsAsync(request.AccountId.Value);
      if (!accountExists)
        return ServiceResult.Failure($"Account with ID {request.AccountId.Value} was not found.", 400);
    }

    var deal = new Deal
    {
      DealId = Guid.NewGuid(),
      Title = request.Title,
      AccountId = request.AccountId,
      Stage = request.Stage,
      Value = request.Value,
      Probability = request.Probability,
      ExpectedCloseDate = request.ExpectedCloseDate,
      OwnerId = request.OwnerId,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };

    await _repository.AddAsync(deal);

    await _publishEndpoint.Publish(new DealCreated
    {
      DealId = deal.DealId,
      Title = deal.Title,
      AccountId = deal.AccountId,
      Stage = deal.Stage,
      Value = deal.Value
    });

    return ServiceResult.Success(MapToResponse(deal), "Deal created successfully.", 201);
  }

  public async Task<ServiceResult> UpdateDealAsync(Guid id, UpdateDealRequest request)
  {
    var deal = await _repository.GetByIdAsync(id);
    if (deal == null)
      return ServiceResult.Failure("Deal not found.", 404);

    var oldStage = deal.Stage;

    if (request.Title != null) deal.Title = request.Title;
    if (request.AccountId.HasValue) deal.AccountId = request.AccountId;
    if (request.Stage.HasValue) deal.Stage = request.Stage.Value;
    if (request.Value.HasValue) deal.Value = request.Value.Value;
    if (request.Probability.HasValue) deal.Probability = request.Probability;
    if (request.ExpectedCloseDate.HasValue) deal.ExpectedCloseDate = request.ExpectedCloseDate;
    if (request.OwnerId.HasValue) deal.OwnerId = request.OwnerId;
    deal.UpdatedAt = DateTime.UtcNow;

    await _repository.UpdateAsync(deal);

    if (request.Stage.HasValue && request.Stage.Value != oldStage)
    {
      await _publishEndpoint.Publish(new DealStageChanged
      {
        DealId = deal.DealId,
        OldStage = oldStage,
        NewStage = deal.Stage
      });

      if (deal.Stage == DealStage.ClosedWon || deal.Stage == DealStage.ClosedLost)
      {
        await _publishEndpoint.Publish(new DealClosed
        {
          DealId = deal.DealId,
          Stage = deal.Stage,
          Value = deal.Value
        });
      }
    }

    return ServiceResult.Success(MapToResponse(deal));
  }

  public async Task<ServiceResult> DeleteDealAsync(Guid id)
  {
    var deal = await _repository.GetByIdAsync(id);
    if (deal == null)
      return ServiceResult.Failure("Deal not found.", 404);

    await _repository.DeleteAsync(id);
    return ServiceResult.Success(statusCode: 204);
  }

  public async Task<ServiceResult> AddContactToDealAsync(Guid dealId, AddDealContactRequest request)
  {
    var deal = await _repository.GetByIdAsync(dealId);
    if (deal == null)
      return ServiceResult.Failure("Deal not found.", 404);

    var contactExists = await _contactClient.ContactExistsAsync(request.ContactId);
    if (!contactExists)
      return ServiceResult.Failure($"Contact with ID {request.ContactId} was not found.", 400);

    var existing = await _repository.GetDealContactAsync(dealId, request.ContactId);
    if (existing != null)
      return ServiceResult.Failure("Contact is already associated with this deal.", 409);

    var dealContact = new DealContact
    {
      DealContactId = Guid.NewGuid(),
      DealId = dealId,
      ContactId = request.ContactId,
      Role = request.Role,
      CreatedAt = DateTime.UtcNow
    };

    await _repository.AddDealContactAsync(dealContact);
    return ServiceResult.Success(new DealContactResponse
    {
      DealContactId = dealContact.DealContactId,
      ContactId = dealContact.ContactId,
      Role = dealContact.Role
    }, "Contact added to deal.", 201);
  }

  public async Task<ServiceResult> RemoveContactFromDealAsync(Guid dealId, Guid contactId)
  {
    var existing = await _repository.GetDealContactAsync(dealId, contactId);
    if (existing == null)
      return ServiceResult.Failure("Contact is not associated with this deal.", 404);

    await _repository.RemoveDealContactAsync(existing.DealContactId);
    return ServiceResult.Success(statusCode: 204);
  }

  private static DealResponse MapToResponse(Deal deal) => new()
  {
    DealId = deal.DealId,
    Title = deal.Title,
    AccountId = deal.AccountId,
    Stage = deal.Stage,
    Value = deal.Value,
    Probability = deal.Probability,
    ExpectedCloseDate = deal.ExpectedCloseDate,
    OwnerId = deal.OwnerId,
    CreatedAt = deal.CreatedAt,
    UpdatedAt = deal.UpdatedAt,
    Contacts = deal.DealContacts.Select(dc => new DealContactResponse
    {
      DealContactId = dc.DealContactId,
      ContactId = dc.ContactId,
      Role = dc.Role
    }).ToList()
  };
}
