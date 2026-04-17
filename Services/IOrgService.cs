using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;

namespace CrmWebApi.Services;

public interface IOrgService
{
	public Task<Result<PagedResponse<OrgResponse>>> GetAllAsync(
		int page,
		int pageSize,
		string? search
	);
	public Task<Result<OrgResponse>> GetByIdAsync(int id);
	public Task<Result<OrgResponse>> CreateAsync(CreateOrgRequest req);
	public Task<Result<OrgResponse>> UpdateAsync(int id, UpdateOrgRequest req);
	public Task<Result> DeleteAsync(int id);
	public Task<Result<IEnumerable<OrgTypeResponse>>> GetAllTypesAsync();
}
