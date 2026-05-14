using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs;
using CrmWebApi.DTOs.Org;
using CrmWebApi.DTOs.OrgType;

namespace CrmWebApi.Repositories;

public interface IOrgRepository
{
	public Task<PagedResponse<OrgResponse>> GetPagedAsync(
		int page,
		int pageSize,
		string? search,
		bool includeTotal
	);
	public Task<OrgResponse?> GetResponseByIdAsync(int id);
	public Task<OrgAuditSnapshot?> GetAuditSnapshotAsync(int id);
	public Task<Organization> AddAsync(Organization entity);
	public Task<int> UpdateAsync(int id, UpdateOrgRequest req);
	public Task<int> SoftDeleteAsync(int id);
	public Task<IReadOnlyList<OrgTypeResponse>> GetOrgTypesAsync(CancellationToken ct = default);
}

public sealed record OrgAuditSnapshot(
	int OrgTypeId,
	string OrgName,
	string OrgInn,
	double? OrgLatitude,
	double? OrgLongitude,
	string OrgAddress
);
