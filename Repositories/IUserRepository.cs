using CrmWebApi.Data.Entities;

namespace CrmWebApi.Repositories;

public interface IUserRepository
{
	public IQueryable<Usr> QueryForScope(Common.Scope scope);
	public IQueryable<Usr> QueryWithPolicies();
	public IQueryable<Usr> QueryForRead();
	public IQueryable<Usr> QueryForUpdate();
	public Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<Usr, bool>> predicate);
	public Task<Usr> AddAsync(Usr entity);
	public Task<Usr> AddWithPoliciesAsync(Usr entity, IEnumerable<int> policyIds);
	public Task UpdateAsync(Usr entity);
	public Task LinkPolicyAsync(int userId, int policyId);
	public Task UnlinkPolicyAsync(int userId, int policyId);
	public IQueryable<Policy> QueryPolicies();
}
