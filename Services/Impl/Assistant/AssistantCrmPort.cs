using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.Services.Assistant;

namespace CrmWebApi.Services.Impl.Assistant;

public sealed class AssistantCrmPort(
	IDrugService drugService,
	IOrgService orgService,
	IPhysService physService,
	IActivService activService
) : IAssistantCrmReadPort, IAssistantCrmWritePort
{
	public Task<Result<PagedResponse<DrugResponse>>> SearchDrugsAsync(string query, int limit) =>
		drugService.GetAllAsync(1, limit, query, includeTotal: false);

	public Task<Result<DrugResponse>> GetDrugAsync(int id) =>
		drugService.GetByIdAsync(id);

	public Task<Result<PagedResponse<OrgResponse>>> SearchOrgsAsync(string query, int limit) =>
		orgService.GetAllAsync(1, limit, query, includeTotal: false);

	public Task<Result<OrgResponse>> GetOrgAsync(int id) =>
		orgService.GetByIdAsync(id);

	public Task<Result<PagedResponse<PhysResponse>>> SearchPhysesAsync(string query, int limit) =>
		physService.GetAllAsync(1, limit, query, includeTotal: false);

	public Task<Result<PhysResponse>> GetPhysAsync(int id) =>
		physService.GetByIdAsync(id);

	public Task<Result<PagedResponse<ActivResponse>>> ListActivsAsync(ActivQuery query) =>
		activService.GetAllAsync(query);

	public Task<Result<ActivResponse>> GetActivAsync(int id) =>
		activService.GetByIdAsync(id);

	public Task<Result<ActivResponse>> CreateActivAsync(CreateActivRequest request) =>
		activService.CreateAsync(request);
}
