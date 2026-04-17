using CrmWebApi.Common;

namespace CrmWebApi.DTOs.Activ;

public class ActivQuery
{
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 100;
	public string? Search { get; set; }
	public ActivSortBy? SortBy { get; set; }
	public bool SortDesc { get; set; }
	public int[]? Statuses { get; set; }
	public DateTimeOffset? DateFrom { get; set; }
	public DateTimeOffset? DateTo { get; set; }
}
