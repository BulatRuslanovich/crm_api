using CrmWebApi.Data.Entities;

namespace CrmWebApi.DTOs.User;

public record UserResponse(
	int UsrId,
	string FirstName,
	string LastName,
	string Email,
	string Login,
	List<string> Policies
)
{
	public static UserResponse From(Usr u) =>
		new(
			u.UsrId,
			u.UsrFirstname,
			u.UsrLastname,
			u.UsrEmail,
			u.UsrLogin,
			[.. u.UsrPolicies.Where(p => p.Policy is not null).Select(p => p.Policy.PolicyName)]
		);
}
