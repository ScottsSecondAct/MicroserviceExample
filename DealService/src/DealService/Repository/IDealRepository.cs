using DealService.Models;
using SharedLibrary.Deals.Enums;

namespace DealService.Repository;

public interface IDealRepository
{
  Task<Deal?> GetByIdAsync(Guid id);
  Task<List<Deal>> GetAllAsync(DealStage? stage = null, Guid? accountId = null, Guid? ownerId = null);
  Task AddAsync(Deal deal);
  Task UpdateAsync(Deal deal);
  Task DeleteAsync(Guid id);
  Task<DealContact?> GetDealContactAsync(Guid dealId, Guid contactId);
  Task AddDealContactAsync(DealContact dealContact);
  Task RemoveDealContactAsync(Guid dealContactId);
  Task RemoveDealContactsByContactIdAsync(Guid contactId);
}
