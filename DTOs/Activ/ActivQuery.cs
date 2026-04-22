using CrmWebApi.Common;

namespace CrmWebApi.DTOs.Activ;

public record ActivQuery(
	string? Search,
	ActivSortBy? SortBy,
	bool SortDesc,
	int[]? Statuses,
	DateTimeOffset? DateFrom,
	DateTimeOffset? DateTo,
	int? UsrId)
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 100;
	public bool IncludeTotal { get; init; } = true;
}
