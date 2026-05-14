using CrmWebApi.Data.Entities;

namespace CrmWebApi.Tests;

internal static class TestUsers
{
	public static Usr UserWithRole(int id, string role) =>
		new()
		{
			UsrId = id,
			UsrFirstname = "Test",
			UsrLastname = "User",
			UsrEmail = $"user{id}@example.com",
			UsrLogin = $"user{id}",
			UsrPasswordHash = BCrypt.Net.BCrypt.HashPassword("password1"),
			IsEmailConfirmed = true,
			UsrPolicies =
			[
				new UsrPolicy
				{
					UsrId = id,
					PolicyId = 1,
					Policy = new Policy { PolicyId = 1, PolicyName = role },
				},
			],
		};
}
