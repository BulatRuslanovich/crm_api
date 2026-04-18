using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;

namespace CrmWebApi.Services;

public interface IActivService
{
	public Task<Result<PagedResponse<ActivResponse>>> GetAllAsync(
		ActivQuery query,
		Scope scope
	);

	public Task<Result<ActivResponse>> GetByIdAsync(int id, Scope scope);
	public Task<Result<ActivResponse>> CreateAsync(int usrId, CreateActivRequest req);
	public Task<Result<ActivResponse>> UpdateAsync(int id, UpdateActivRequest req, Scope scope);
	public Task<Result> DeleteAsync(int id, Scope scope);
	public Task<Result> LinkDrugAsync(int activId, int drugId, Scope scope);
	public Task<Result> UnlinkDrugAsync(int activId, int drugId, Scope scope);
}
