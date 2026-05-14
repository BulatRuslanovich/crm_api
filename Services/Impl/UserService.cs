using CrmWebApi.Common;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;
using CrmWebApi.Repositories;

namespace CrmWebApi.Services.Impl;

public class UserService(
	IUserRepository repo,
	IAuthSessionService sessionService,
	IPasswordHasher passwordHasher,
	ICurrentUserService currentUser
) : IUserService
{
	public async Task<Result<PagedResponse<UserResponse>>> GetAllAsync(
		int page,
		int pageSize,
		bool includeTotal = true
	)
	{
		if (currentUser.Scope is not { } scope)
			return Error.Forbidden("Доступ запрещён");

		return await repo.GetPagedForScopeAsync(page, pageSize, scope, includeTotal);
	}

	public async Task<Result<UserResponse>> GetByIdAsync(int id)
	{
		var user = await repo.GetByIdWithPoliciesAsync(id);
		if (user is null)
			return Error.NotFound($"Пользователь {id} не найден");
		return UserResponse.From(user);
	}

	public async Task<Result<UserResponse>> CreateAsync(CreateUserRequest req)
	{
		var loginLower = req.Login.ToLower();
		if (await repo.ExistsActiveLoginAsync(loginLower))
			return Error.Conflict("Логин уже занят");

		var user = new Usr
		{
			UsrFirstname = req.FirstName,
			UsrLastname = req.LastName,
			UsrEmail = req.Email,
			UsrLogin = req.Login,
			IsEmailConfirmed = true,
			UsrPasswordHash = passwordHasher.Hash(req.Password),
		};
		await repo.AddWithPoliciesAsync(user, req.PolicyIds.Distinct());

		return await GetByIdAsync(user.UsrId);
	}

	public async Task<Result<UserResponse>> UpdateAsync(int id, UpdateUserRequest req)
	{
		var affected = await repo.UpdateNamesAsync(id, req.FirstName, req.LastName);
		if (affected == 0)
			return Error.NotFound($"Пользователь {id} не найден");

		return await GetByIdAsync(id);
	}

	public async Task<Result> DeleteAsync(int id)
	{
		var affected = await repo.SoftDeleteAsync(id);
		return affected == 0
			? Error.NotFound($"Пользователь {id} не найден")
			: Result.Success();
	}

	public async Task<Result> ChangePasswordAsync(int id, ChangePasswordRequest req)
	{
		var user = await repo.GetByIdForUpdateAsync(id);
		if (user is null)
			return Error.NotFound($"Пользователь {id} не найден");

		if (!passwordHasher.Verify(req.OldPassword, user.UsrPasswordHash))
			return Error.Unauthorized("Неверный текущий пароль");

		user.UsrPasswordHash = passwordHasher.Hash(req.NewPassword);
		await repo.UpdateAsync(user);
		await sessionService.RevokeAllForUserAsync(id);
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
		Result<IEnumerable<PolicyResponse>>.Success(await repo.GetPoliciesAsync());

	public async Task<Result<PolicyResponse>> GetPolicyByIdAsync(int id)
	{
		var policy = await repo.GetPolicyByIdAsync(id);
		if (policy is null)
			return Error.NotFound($"Политика {id} не найдена");
		return policy;
	}
}
