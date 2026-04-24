using CrmWebApi.Common;

namespace CrmWebApi.DTOs.Activ;

public record ActivQuery(
	string? Search,
	ActivSortBy? SortBy,
	bool SortDesc,
	int[]? Statuses,
	int? UsrId)
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 100;
	public DateTimeOffset? DateFrom { get; set; }
	public DateTimeOffset? DateTo { get; set; }
	public bool IncludeTotal { get; init; } = true;
}
