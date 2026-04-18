namespace CrmWebApi.Common;

public enum Visibility
{
	All,
	Department,
	Own,
}

public readonly record struct Scope(int CurrentUsrId, Visibility Visibility)
{
	public static Scope ForAll(int usrId) => new(usrId, Visibility.All);

	public static Scope ForDepartment(int usrId) => new(usrId, Visibility.Department);

	public static Scope ForOwn(int usrId) => new(usrId, Visibility.Own);
}

