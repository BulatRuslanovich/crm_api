using System.Security.Claims;
using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController(IUserService service) : ApiController
{
	[HttpGet("policies")]
	[EndpointSummary("List all policies")]
	[ProducesResponseType<IEnumerable<PolicyResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAllPolicies() =>
		FromResult(await service.GetAllPoliciesAsync());

	[HttpGet("policies/{id:int}")]
	[EndpointSummary("Get policy by ID")]
	[ProducesResponseType<PolicyResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetPolicyById(int id) =>
		FromResult(await service.GetPolicyByIdAsync(id));

	[HttpGet]
	[Authorize(Roles = RoleNames.AdminManagerDirector)]
	[EndpointSummary("List all users")]
	[ProducesResponseType<PagedResponse<UserResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20,
		[FromQuery] bool includeTotal = true
	)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;

		return FromResult(
			await service.GetAllAsync(
				Math.Max(page, 1),
				Math.Clamp(pageSize, 1, 1000),
				scope,
				includeTotal
			)
		);
	}


	[HttpGet("{id:int}")]
	[EndpointSummary("Get user by ID")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetById(int id)
	{
		var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		if (id != currentUserId && !User.IsInRole(RoleNames.Admin))
			return ForbiddenProblem();
		return FromResult(await service.GetByIdAsync(id));
	}

	[HttpGet("me")]
	[EndpointSummary("Get current user profile")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetMe()
	{
		var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		return FromResult(await service.GetByIdAsync(id));
	}

	[HttpPost]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Create user")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
	public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
	{
		var result = await service.CreateAsync(request);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.UsrId });
	}

	[HttpPut("{id:int}")]
	[Authorize]
	[EndpointSummary("Update user")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
	{
		var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		if (id != currentUserId && !User.IsInRole(RoleNames.Admin))
			return ForbiddenProblem();

		return FromResult(await service.UpdateAsync(id, request));
	}

	[HttpPatch("{id:int}/password")]
	[EndpointSummary("Change password")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ChangePassword(
		int id,
		[FromBody] ChangePasswordRequest request
	)
	{
		var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		if (id != currentUserId && !User.IsInRole(RoleNames.Admin))
			return ForbiddenProblem();
		return FromResult(await service.ChangePasswordAsync(id, request));
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Delete user")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(int id) => FromResult(await service.DeleteAsync(id));

	[HttpPost("{id:int}/policies/{policyId:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Link policy to user")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> LinkPolicy(int id, int policyId) =>
		FromResult(await service.LinkPolicyAsync(id, policyId));

	[HttpDelete("{id:int}/policies/{policyId:int}")]
	[Authorize(Roles = RoleNames.Admin)]
	[EndpointSummary("Unlink policy from user")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> UnlinkPolicy(int id, int policyId) =>
		FromResult(await service.UnlinkPolicyAsync(id, policyId));

}
