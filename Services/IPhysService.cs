using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Spec;

namespace CrmWebApi.Services;

public interface IPhysService
{
	public Task<Result<PagedResponse<PhysResponse>>> GetAllAsync(
		int page,
		int pageSize,
		string? search
	);
	public Task<Result<PhysResponse>> GetByIdAsync(int id);
	public Task<Result<PhysResponse>> CreateAsync(CreatePhysRequest req);
	public Task<Result<PhysResponse>> UpdateAsync(int id, UpdatePhysRequest req);
	public Task<Result> DeleteAsync(int id);
	public Task<Result> LinkOrgAsync(int physId, int orgId);
	public Task<Result> UnlinkOrgAsync(int physId, int orgId);
	public Task<Result<IEnumerable<SpecResponse>>> GetAllSpecsAsync();
	public Task<Result<SpecResponse>> GetSpecByIdAsync(int id);
	public Task<Result<SpecResponse>> CreateSpecAsync(CreateSpecRequest req);
	public Task<Result> DeleteSpecAsync(int id);
}
