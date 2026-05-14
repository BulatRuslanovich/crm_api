using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;

namespace CrmWebApi.Repositories;

public interface IActivRepository
{
	public Task<PagedResponse<ActivResponse>> GetPagedForScopeAsync(ActivQuery query, Scope scope);
	public Task<ActivResponse?> GetResponseByIdForScopeAsync(int id, Scope scope);
	public Task<Activ?> GetForUpdateAsync(int id, Scope scope);
	public Task<bool> ExistsInScopeAsync(int id, Scope scope);
	public Task<Activ> AddAsync(Activ entity);
	public Task UpdateAsync(Activ entity);
	public Task<int> SoftDeleteAsync(int id, Scope scope);
	public Task LinkDrugAsync(int activId, int drugId);
	public Task<bool> UnlinkDrugAsync(int activId, int drugId);
}
