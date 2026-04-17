using CrmWebApi.Common;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Drug;

namespace CrmWebApi.Services;

public interface IDrugService
{
	public Task<Result<PagedResponse<DrugResponse>>> GetAllAsync(
		int page,
		int pageSize,
		string? search
	);
	public Task<Result<DrugResponse>> GetByIdAsync(int id);
	public Task<Result<DrugResponse>> CreateAsync(CreateDrugRequest req);
	public Task<Result<DrugResponse>> UpdateAsync(int id, UpdateDrugRequest req);
	public Task<Result> DeleteAsync(int id);
}
