using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Activ;

namespace CrmWebApi.Services;

public interface IActivService
{
	public Task<Result<PagedResponse<ActivResponse>>> GetAllAsync(
		ActivQuery query,
		ActivScope scope
	);

	public Task<Result<ActivResponse>> GetByIdAsync(int id, ActivScope scope);
	public Task<Result<ActivResponse>> CreateAsync(int usrId, CreateActivRequest req);
	public Task<Result<ActivResponse>> UpdateAsync(int id, UpdateActivRequest req, ActivScope scope);
	public Task<Result> DeleteAsync(int id, ActivScope scope);
	public Task<Result> LinkDrugAsync(int activId, int drugId, ActivScope scope);
	public Task<Result> UnlinkDrugAsync(int activId, int drugId, ActivScope scope);
}
