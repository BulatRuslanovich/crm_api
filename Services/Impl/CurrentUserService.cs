using System.Security.Claims;
using CrmWebApi.Common;

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

	public Scope? Scope
	{
		get
		{
			if (UsrId is not { } usrId) return null;
			var user = httpCtx.HttpContext?.User;
			if (user is null) return null;

			if (user.IsInRole(RoleNames.Admin) || user.IsInRole(RoleNames.Director))
				return Common.Scope.ForAll(usrId);
			if (user.IsInRole(RoleNames.Manager))
				return Common.Scope.ForDepartment(usrId);
			return Common.Scope.ForOwn(usrId);
		}
	}
}
