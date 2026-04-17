using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Policy;
using CrmWebApi.DTOs.User;

namespace CrmWebApi.Services;

public interface IUserService
{
	public Task<Result<PagedResponse<UserResponse>>> GetAllAsync(int page, int pageSize);
	public Task<Result<UserResponse>> GetByIdAsync(int id);
	public Task<Result<UserResponse>> CreateAsync(CreateUserRequest request);
	public Task<Result<UserResponse>> UpdateAsync(int id, UpdateUserRequest request);
	public Task<Result> DeleteAsync(int id);
	public Task<Result> ChangePasswordAsync(int id, ChangePasswordRequest request);
	public Task<Result<UserResponse>> LinkPolicyAsync(int userId, int policyId);
	public Task<Result<UserResponse>> UnlinkPolicyAsync(int userId, int policyId);
	public Task<Result<IEnumerable<PolicyResponse>>> GetAllPoliciesAsync();
	public Task<Result<PolicyResponse>> GetPolicyByIdAsync(int id);
}
