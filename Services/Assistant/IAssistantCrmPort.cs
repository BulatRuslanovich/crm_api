using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.Phys;

namespace CrmWebApi.Services.Assistant;

public interface IAssistantCrmReadPort
{
	public Task<Result<PagedResponse<DrugResponse>>> SearchDrugsAsync(string query, int limit);
	public Task<Result<DrugResponse>> GetDrugAsync(int id);
	public Task<Result<PagedResponse<OrgResponse>>> SearchOrgsAsync(string query, int limit);
	public Task<Result<OrgResponse>> GetOrgAsync(int id);
	public Task<Result<PagedResponse<PhysResponse>>> SearchPhysesAsync(string query, int limit);
	public Task<Result<PhysResponse>> GetPhysAsync(int id);
	public Task<Result<PagedResponse<ActivResponse>>> ListActivsAsync(ActivQuery query);
	public Task<Result<ActivResponse>> GetActivAsync(int id);
}

public interface IAssistantCrmWritePort
{
	public Task<Result<ActivResponse>> CreateActivAsync(CreateActivRequest request);
}
