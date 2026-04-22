using CrmWebApi.Common;
using CrmWebApi.Data.Entities;

namespace CrmWebApi.Repositories;

public interface IActivRepository
{
	public IQueryable<Activ> QueryForScope(Scope scope);
	public Task<Activ> AddWithDrugsAsync(Activ entity, IEnumerable<int> drugIds);
	public Task UpdateAsync(Activ entity);
	public Task LinkDrugAsync(int activId, int drugId);
	public Task<bool> UnlinkDrugAsync(int activId, int drugId);
}
