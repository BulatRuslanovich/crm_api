using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;
using CrmWebApi.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace CrmWebApi.Services.Impl;

public class UserService(
	IUserRepository repo,
	IAuthSessionService sessionService,
	IPasswordHasher passwordHasher,
	HybridCache cache
) : IUserService
{
	private static readonly string[] PolicyTags = ["policies"];
	private static readonly HybridCacheEntryOptions RefOptions = new()
	{
		Expiration = TimeSpan.FromMinutes(10),
	};

	public async Task<Result<PagedResponse<UserResponse>>> GetAllAsync(
		int page,
		int pageSize,
		Scope scope,
		bool includeTotal = true
	)
	{
		var query = repo.QueryForScope(scope);
		var total = includeTotal ? await query.CountAsync() : 0;
		var entities = await query
			.OrderBy(u => u.UsrId)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();
		var responses = entities.Select(UserResponse.From).ToList();

		return new PagedResponse<UserResponse>(responses, page, pageSize, total);
	}

	public async Task<Result<UserResponse>> GetByIdAsync(int id)
	{
		var user = await repo.QueryHard().FirstOrDefaultAsync(u => u.UsrId == id);
		if (user is null)
			return Error.NotFound($"Пользователь {id} не найден");
		return UserResponse.From(user);
	}

	public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest req)
	{
		if (await repo.ExistsAsync(u => u.UsrLogin == req.Login))
			return Error.Conflict("Логин уже занят");

		var user = new Usr
		{
			UsrFirstname = req.FirstName,
			UsrLastname = req.LastName,
			UsrEmail = req.Email,
			UsrLogin = req.Login,
			UsrPasswordHash = passwordHasher.Hash(req.Password),
		};
		await repo.AddWithPoliciesAsync(user, req.PolicyIds.Distinct());

		return await GetByIdAsync(user.UsrId);
	}

	public async Task<Result<UserResponse>> UpdateAsync(int id, UpdateUserRequest req)
	{
		var user = await repo.QueryForUpdate().FirstOrDefaultAsync(u => u.UsrId == id);
		if (user is null)
			return Error.NotFound($"Пользователь {id} не найден");

		user.UsrFirstname = req.FirstName ?? user.UsrFirstname;
		user.UsrLastname = req.LastName ?? user.UsrLastname;

		await repo.UpdateAsync(user);
		return await GetByIdAsync(id);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var user = await repo.QueryForUpdate().FirstOrDefaultAsync(u => u.UsrId == id);
		if (user is null)
			return Error.NotFound($"Пользователь {id} не найден");

		user.IsDeleted = true;
		await repo.UpdateAsync(user);
		await sessionService.RevokeAllForUserAsync(id);
		return Result.Success();
	}

	public async Task<Result> ChangePasswordAsync(int id, ChangePasswordRequest req)
	{
		var user = await repo.QueryForUpdate().FirstOrDefaultAsync(u => u.UsrId == id);
		if (user is null)
			return Error.NotFound($"Пользователь {id} не найден");

		if (!passwordHasher.Verify(req.OldPassword, user.UsrPasswordHash))
			return Error.Unauthorized("Неверный текущий пароль");

		user.UsrPasswordHash = passwordHasher.Hash(req.NewPassword);
		await repo.UpdateAsync(user);
		return Result.Success();
	}

	public async Task<Result<UserResponse>> LinkPolicyAsync(int userId, int policyId)
	{
		await repo.LinkPolicyAsync(userId, policyId);
		return await GetByIdAsync(userId);
	}

	public async Task<Result<UserResponse>> UnlinkPolicyAsync(int userId, int policyId)
	{
		await repo.UnlinkPolicyAsync(userId, policyId);
		return await GetByIdAsync(userId);
	}

	public async Task<Result<IEnumerable<PolicyResponse>>> GetAllPoliciesAsync() =>
		Result<IEnumerable<PolicyResponse>>.Success(
			await cache.GetOrCreateAsync(
				"policies",
				async ct =>
					(IEnumerable<PolicyResponse>)
						await repo.QueryPolicies()
							.Select(p => new PolicyResponse(p.PolicyId, p.PolicyName))
							.ToListAsync(ct),
				RefOptions,
				PolicyTags
			)
		);

	public async Task<Result<PolicyResponse>> GetPolicyByIdAsync(int id)
	{
		var policy = await repo.QueryPolicies().FirstOrDefaultAsync(p => p.PolicyId == id);
		if (policy is null)
			return Error.NotFound($"Политика {id} не найдена");
		return new PolicyResponse(policy.PolicyId, policy.PolicyName);
	}
}
