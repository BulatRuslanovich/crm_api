using System.Linq.Expressions;
using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class UserRepository(AppDbContext db) : IUserRepository
{
	public IQueryable<Usr> QueryForScope(Scope scope)
	{
		var baseQuery = QueryHard();
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


	public IQueryable<Usr> QueryHard() =>
		db
			.Usrs.Where(u => !u.IsDeleted)
			.Include(u => u.UsrPolicies)
				.ThenInclude(up => up.Policy)
			.AsSplitQuery()
			.AsNoTracking();

	public IQueryable<Usr> QueryLite() => db.Usrs.Where(u => !u.IsDeleted).AsNoTracking();

	public IQueryable<Usr> QueryForUpdate() => db.Usrs.Where(u => !u.IsDeleted);

	public Task<bool> ExistsAsync(Expression<Func<Usr, bool>> predicate) =>
		db.Usrs.AnyAsync(predicate);

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

	public async Task AddPoliciesAsync(IEnumerable<UsrPolicy> policies)
	{
		await db.UsrPolicies.AddRangeAsync(policies);
		await db.SaveChangesAsync();
	}

	public async Task LinkPolicyAsync(int userId, int policyId)
	{
		var exists = await db.UsrPolicies.AnyAsync(up =>
			up.UsrId == userId && up.PolicyId == policyId
		);
		if (!exists)
		{
			db.UsrPolicies.Add(new UsrPolicy { UsrId = userId, PolicyId = policyId });
			await db.SaveChangesAsync();
		}
	}

	public async Task UnlinkPolicyAsync(int userId, int policyId) =>
		await db
			.UsrPolicies.Where(up => up.UsrId == userId && up.PolicyId == policyId)
			.ExecuteDeleteAsync();

	public IQueryable<Policy> QueryPolicies() => db.Policies.AsNoTracking();
}
