using CrmWebApi.Data.Entities;

namespace CrmWebApi.Repositories;

public interface IOrgRepository
{
	public IQueryable<Organization> QueryHard();
	public IQueryable<Organization> QueryLite();
	public IQueryable<OrgType> QueryOrgTypes();
	public Task<Organization> AddAsync(Organization entity);
	public Task UpdateAsync(Organization entity);
}
