namespace CrmWebApi.DTOs;

public record PagedResponse<T>(IEnumerable<T> Items, int Page, int PageSize, int TotalCount)
{
	public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
