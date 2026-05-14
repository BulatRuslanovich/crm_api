using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;

namespace CrmWebApi.Repositories;

public interface IUserRepository
{
	public Task<PagedResponse<UserResponse>> GetPagedForScopeAsync(
		int page,
		int pageSize,
		Scope scope,
		bool includeTotal
	);
	public Task<Usr?> GetByIdWithPoliciesAsync(int id);
	public Task<Usr?> GetByIdForUpdateAsync(int id);
	public Task<Usr?> GetByLoginWithPoliciesAsync(string loginLower);
	public Task<Usr?> GetByEmailForUpdateAsync(string emailLower);
	public Task<Usr?> GetConfirmedByEmailAsync(string emailLower);
	public Task<bool> ExistsActiveByLoginOrEmailAsync(string loginLower, string emailLower);
	public Task<bool> ExistsActiveLoginAsync(string loginLower);
	public Task<Usr> AddAsync(Usr entity);
	public Task<Usr> AddWithPoliciesAsync(Usr entity, IEnumerable<int> policyIds);
	public Task UpdateAsync(Usr entity);
	public Task<int> UpdateNamesAsync(int id, string? firstName, string? lastName);
	public Task<int> SoftDeleteAsync(int id);
	public Task LinkPolicyAsync(int userId, int policyId);
	public Task UnlinkPolicyAsync(int userId, int policyId);
	public Task<IReadOnlyList<PolicyResponse>> GetPoliciesAsync(CancellationToken ct = default);
	public Task<PolicyResponse?> GetPolicyByIdAsync(int id);
}
