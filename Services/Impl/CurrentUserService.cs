using System.Security.Claims;

namespace CrmWebApi.Services.Impl;

public sealed class CurrentUserService(IHttpContextAccessor httpCtx) : ICurrentUserService
{
	public int? UsrId
	{
		get
		{
			var claim = httpCtx.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
			return int.TryParse(claim, out var id) ? id : null;
		}
	}
}
