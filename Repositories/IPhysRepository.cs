using CrmWebApi.Data.Entities;

namespace CrmWebApi.Repositories;

public interface IPhysRepository
{
	public IQueryable<Phys> QueryHard();
	public IQueryable<Phys> QueryLite();
	public Task<Phys> AddAsync(Phys entity);
	public Task UpdateAsync(Phys entity);
	public Task LinkOrgAsync(int physId, int orgId);
	public Task<bool> UnlinkOrgAsync(int physId, int orgId);

	public IQueryable<Spec> QuerySpecs();
	public Task<Spec> AddSpecAsync(Spec entity);
	public Task<bool> SoftDeleteSpecAsync(int id);
}
