using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Phys;
using CrmWebApi.DTOs.Spec;

namespace CrmWebApi.Repositories;

public interface IPhysRepository
{
	public Task<PagedResponse<PhysResponse>> GetPagedAsync(
		int page,
		int pageSize,
		string? search,
		bool includeTotal
	);
	public Task<PhysResponse?> GetResponseByIdAsync(int id);
	public Task<PhysAuditSnapshot?> GetAuditSnapshotAsync(int id);
	public Task<Phys> AddAsync(Phys entity);
	public Task<int> UpdateAsync(int id, UpdatePhysRequest req);
	public Task<int> SoftDeleteAsync(int id);
	public Task LinkOrgAsync(int physId, int orgId);
	public Task<bool> UnlinkOrgAsync(int physId, int orgId);
	public Task<IReadOnlyList<SpecResponse>> GetSpecsAsync(CancellationToken ct = default);
	public Task<SpecResponse?> GetSpecByIdAsync(int id);
	public Task<Spec> AddSpecAsync(Spec entity);
	public Task<bool> SoftDeleteSpecAsync(int id);
}

public sealed record PhysAuditSnapshot(
	int? SpecId,
	string PhysFirstname,
	string PhysLastname,
	string? PhysMiddlename,
	string? PhysPhone,
	string PhysEmail
);
