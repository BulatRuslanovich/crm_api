using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class UserRepository(AppDbContext db) : IUserRepository
{
	public async Task<PagedResponse<UserResponse>> GetPagedForScopeAsync(
		int page,
		int pageSize,
		Scope scope,
		bool includeTotal
	)
	{
		var query = QueryForScope(scope);
		var total = includeTotal ? await query.CountAsync() : 0;
		var responses = await query
			.OrderBy(u => u.UsrId)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(u => new UserResponse(
				u.UsrId,
				u.UsrFirstname,
				u.UsrLastname,
				u.UsrEmail,
				u.UsrLogin,
				u.UsrPolicies.Select(p => p.Policy.PolicyName).ToList()
			))
			.ToListAsync();

		return new PagedResponse<UserResponse>(responses, page, pageSize, total);
	}

	public Task<Usr?> GetByIdWithPoliciesAsync(int id) =>
		QueryWithPolicies().FirstOrDefaultAsync(u => u.UsrId == id);

	public Task<Usr?> GetByIdForUpdateAsync(int id) =>
		db.Usrs.Where(u => !u.IsDeleted).FirstOrDefaultAsync(u => u.UsrId == id);

	public Task<Usr?> GetByLoginWithPoliciesAsync(string loginLower) =>
		QueryWithPolicies().FirstOrDefaultAsync(u => u.UsrLogin.ToLower() == loginLower);

	public Task<Usr?> GetByEmailForUpdateAsync(string emailLower) =>
		db.Usrs.Where(u => !u.IsDeleted).FirstOrDefaultAsync(u => u.UsrEmail.ToLower() == emailLower);

	public Task<Usr?> GetConfirmedByEmailAsync(string emailLower) =>
		db.Usrs
			.Where(u => !u.IsDeleted)
			.AsNoTracking()
			.FirstOrDefaultAsync(u => u.UsrEmail.ToLower() == emailLower && u.IsEmailConfirmed);

	public Task<bool> ExistsActiveByLoginOrEmailAsync(string loginLower, string emailLower) =>
		db.Usrs.AnyAsync(u =>
			(u.UsrLogin.ToLower() == loginLower || u.UsrEmail.ToLower() == emailLower)
			&& !u.IsDeleted
		);

	public Task<bool> ExistsActiveLoginAsync(string loginLower) =>
		db.Usrs.AnyAsync(u => u.UsrLogin.ToLower() == loginLower && !u.IsDeleted);

	public async Task<Usr> AddAsync(Usr entity)
	{
		db.Usrs.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task<Usr> AddWithPoliciesAsync(Usr entity, IEnumerable<int> policyIds)
	{
		foreach (var policyId in policyIds)
			entity.UsrPolicies.Add(new UsrPolicy { PolicyId = policyId });
		db.Usrs.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task UpdateAsync(Usr entity)
	{
		if (db.Entry(entity).State == EntityState.Detached)
			db.Usrs.Update(entity);
		await db.SaveChangesAsync();
	}

	public Task<int> UpdateNamesAsync(int id, string? firstName, string? lastName) =>
		db.Usrs
			.Where(u => !u.IsDeleted && u.UsrId == id)
			.ExecuteUpdateAsync(s => s
				.SetProperty(u => u.UsrFirstname, u => firstName ?? u.UsrFirstname)
				.SetProperty(u => u.UsrLastname, u => lastName ?? u.UsrLastname));

	public Task<int> SoftDeleteAsync(int id) =>
		db.Usrs
			.Where(u => !u.IsDeleted && u.UsrId == id)
			.ExecuteUpdateAsync(s => s.SetProperty(u => u.IsDeleted, true));

	public Task LinkPolicyAsync(int userId, int policyId) =>
		db.Database.ExecuteSqlInterpolatedAsync(
			$"INSERT INTO usr_policy (usr_id, policy_id) VALUES ({userId}, {policyId}) ON CONFLICT DO NOTHING"
		);

	public async Task UnlinkPolicyAsync(int userId, int policyId) =>
		await db
			.UsrPolicies.Where(up => up.UsrId == userId && up.PolicyId == policyId)
			.ExecuteDeleteAsync();

	public async Task<IReadOnlyList<PolicyResponse>> GetPoliciesAsync(CancellationToken ct = default) =>
		await db.Policies
			.AsNoTracking()
			.Select(p => new PolicyResponse(p.PolicyId, p.PolicyName))
			.ToListAsync(ct);

	public Task<PolicyResponse?> GetPolicyByIdAsync(int id) =>
		db.Policies
			.AsNoTracking()
			.Where(p => p.PolicyId == id)
			.Select(p => new PolicyResponse(p.PolicyId, p.PolicyName))
			.FirstOrDefaultAsync();

	private IQueryable<Usr> QueryForScope(Scope scope)
	{
		var baseQuery = QueryWithPolicies();
		return scope.Visibility switch
		{
			Visibility.All => baseQuery,
			Visibility.Own => baseQuery.Where(u => u.UsrId == scope.CurrentUsrId),
			Visibility.Department => baseQuery.Where(u =>
				u.UsrId == scope.CurrentUsrId
				|| u.UsrDepartments.Any(ud =>
					ud.Department.UsrDepartments.Any(mine => mine.UsrId == scope.CurrentUsrId)
				)
			),
			_ => baseQuery.Where(_ => false),
		};
	}

	private IQueryable<Usr> QueryWithPolicies() =>
		db
			.Usrs.Where(u => !u.IsDeleted)
			.Include(u => u.UsrPolicies)
			.ThenInclude(up => up.Policy)
			.AsNoTracking();
}
