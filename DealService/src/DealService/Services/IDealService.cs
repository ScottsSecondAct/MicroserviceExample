using DealService.Models.DTOs;
using SharedLibrary.Deals.Enums;

namespace DealService.Services;

public interface IDealService
{
  Task<ServiceResult> GetAllDealsAsync(DealStage? stage = null, Guid? accountId = null, Guid? ownerId = null);
  Task<ServiceResult> GetDealAsync(Guid id);
  Task<ServiceResult> CreateDealAsync(CreateDealRequest request);
  Task<ServiceResult> UpdateDealAsync(Guid id, UpdateDealRequest request);
  Task<ServiceResult> DeleteDealAsync(Guid id);
  Task<ServiceResult> AddContactToDealAsync(Guid dealId, AddDealContactRequest request);
  Task<ServiceResult> RemoveContactFromDealAsync(Guid dealId, Guid contactId);
}
