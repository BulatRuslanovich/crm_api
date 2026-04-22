using CrmWebApi.Common;

namespace CrmWebApi.DTOs.Activ;

public class ActivQuery(
	string? search,
	ActivSortBy? sortBy,
	bool sortDesc,
	int[]? statuses,
	DateTimeOffset? dateFrom,
	DateTimeOffset? dateTo,
	int? usrId)
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 100;
	public string? Search { get; } = search;
	public ActivSortBy? SortBy { get; } = sortBy;
	public bool SortDesc { get; } = sortDesc;
	public int[]? Statuses { get; } = statuses;
	public DateTimeOffset? DateFrom { get; } = dateFrom;
	public DateTimeOffset? DateTo { get; } = dateTo;
	public int? UsrId { get; } = usrId;
	public const bool IncludeTotal = true;
}
