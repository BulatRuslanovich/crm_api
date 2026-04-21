namespace CrmWebApi.Common;

public static class RoleNames
{
	public const string Admin = "Admin";
	public const string Director = "Director";
	public const string Manager = "Manager";
	public const string Representative = "Representative";

	public const string AdminManagerDirector = $"{Admin}, {Manager}, {Director}";
}
