using CrmWebApi.Common;

namespace CrmWebApi.Services;

public interface ICurrentUserService
{
	public int? UsrId { get; }
	public Scope? Scope { get; }
}
