namespace CrmWebApi.Common;

public enum ActivVisibility
{
	All,
	Department,
	Own,
}

public readonly record struct ActivScope(int CurrentUsrId, ActivVisibility Visibility)
{
	public static ActivScope ForAll(int usrId) => new(usrId, ActivVisibility.All);

	public static ActivScope ForDepartment(int usrId) => new(usrId, ActivVisibility.Department);

	public static ActivScope ForOwn(int usrId) => new(usrId, ActivVisibility.Own);
}
