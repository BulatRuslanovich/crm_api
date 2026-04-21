using System.Security.Claims;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/users")]
[Tags("Users")]
[Authorize]
public class UsersController(IUserService service) : ApiController
{
	[HttpGet("policies")]
	[EndpointSummary("List all policies")]
	[EndpointDescription("Returns all available access policies (roles). Cached for 10 minutes.")]
	[ProducesResponseType<IEnumerable<PolicyResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAllPolicies() =>
		FromResult(await service.GetAllPoliciesAsync());

	[HttpGet("policies/{id:int}")]
	[EndpointSummary("Get policy by ID")]
	[ProducesResponseType<PolicyResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetPolicyById(int id) =>
		FromResult(await service.GetPolicyByIdAsync(id));

	[HttpGet]
	[Authorize(Roles = "Admin, Manager, Director")]
	[EndpointSummary("List all users")]
	[EndpointDescription("Paginated list of users with their policies.")]
	[ProducesResponseType<PagedResponse<UserResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetAll(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 20
	)
	{
		if (!TryGetScope(out var scope, out var forbid))
			return forbid!;

		return FromResult(await service.GetAllAsync(Math.Max(page, 1), Math.Clamp(pageSize, 1, 1000), scope));
	}


	[HttpGet("{id:int}")]
	[EndpointSummary("Get user by ID")]
	[EndpointDescription(
		"Returns user profile. Users can only view their own profile unless they are an Admin."
	)]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetById(int id)
	{
		var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		if (id != currentUserId && !User.IsInRole("Admin"))
			return ForbiddenProblem();
		return FromResult(await service.GetByIdAsync(id));
	}

	[HttpGet("me")]
	[EndpointSummary("Get current user profile")]
	[EndpointDescription("Returns the authenticated user's own profile.")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> GetMe()
	{
		var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		return FromResult(await service.GetByIdAsync(id));
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	[EndpointSummary("Create user")]
	[EndpointDescription("Creates a new user with specified policies. Admin only.")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
	{
		var result = await service.CreateAsync(request);
		return CreatedResult(result, nameof(GetById), new { id = result.Value?.UsrId });
	}

	[HttpPut("{id:int}")]
	[Authorize]
	[EndpointSummary("Update user")]
	[EndpointDescription("Updates user profile fields.")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
	{
		var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		if (id != currentUserId && !User.IsInRole("Admin"))
			return ForbiddenProblem();

		return FromResult(await service.UpdateAsync(id, request));
	}

	[HttpPatch("{id:int}/password")]
	[EndpointSummary("Change password")]
	[EndpointDescription(
		"Changes the user's password. Users can only change their own password unless they are an Admin."
	)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> ChangePassword(
		int id,
		[FromBody] ChangePasswordRequest request
	)
	{
		var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
		if (id != currentUserId && !User.IsInRole("Admin"))
			return ForbiddenProblem();
		return FromResult(await service.ChangePasswordAsync(id, request));
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin")]
	[EndpointSummary("Delete user")]
	[EndpointDescription("Soft-deletes a user and revokes all their refresh tokens. Admin only.")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id) => FromResult(await service.DeleteAsync(id));

	[HttpPost("{id:int}/policies/{policyId:int}")]
	[Authorize(Roles = "Admin")]
	[EndpointSummary("Assign policy to user")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> LinkPolicy(int id, int policyId) =>
		FromResult(await service.LinkPolicyAsync(id, policyId));

	[HttpDelete("{id:int}/policies/{policyId:int}")]
	[Authorize(Roles = "Admin")]
	[EndpointSummary("Remove policy from user")]
	[ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> UnlinkPolicy(int id, int policyId) =>
		FromResult(await service.UnlinkPolicyAsync(id, policyId));

}
