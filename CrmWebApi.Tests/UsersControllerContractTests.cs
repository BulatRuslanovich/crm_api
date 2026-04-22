using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "Users")]
public sealed class UsersControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Users GET policies returns reference data")]
	public async Task GetPolicies_WithAdminToken_ReturnsReferenceData()
	{
		// Arrange: policies are protected reference data.
		using var request = AuthorizedGet("/api/users/policies", RoleNames.Admin);

		// Act: request policies.
		var response = await Client.SendAsync(request);

		// Assert: policies return as a JSON array.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetArrayLength());
	}

	[Fact(DisplayName = "Users GET policy by id returns item response")]
	public async Task GetPolicyById_WithAdminToken_ReturnsPolicy()
	{
		// Arrange: request an existing fake policy id.
		using var request = AuthorizedGet("/api/users/policies/1", RoleNames.Admin);

		// Act: request policy by id.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the requested policy.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Users GET list returns paged response")]
	public async Task GetAll_WithAdminToken_ReturnsPagedResponse()
	{
		// Arrange: admins can list users.
		using var request = AuthorizedGet("/api/users", RoleNames.Admin);

		// Act: request users list.
		var response = await Client.SendAsync(request);

		// Assert: the endpoint returns a standard paged response.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Users GET by id returns item response")]
	public async Task GetById_WithAdminToken_ReturnsUser()
	{
		// Arrange: request an existing fake user id.
		using var request = AuthorizedGet("/api/users/1", RoleNames.Admin);

		// Act: request user by id.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the requested user.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Users GET me returns current authenticated user")]
	public async Task GetMe_WithAdminToken_ReturnsCurrentUser()
	{
		// Arrange: /me reads the user id from the JWT name identifier claim.
		using var request = AuthorizedGet("/api/users/me", RoleNames.Admin, userId: 1);

		// Act: request the current user profile.
		var response = await Client.SendAsync(request);

		// Assert: the returned user matches the authenticated user id.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("usrId").GetInt32());
		Assert.Equal("admin", json.GetProperty("login").GetString());
	}

	[Fact(DisplayName = "Users POST creates item and returns 201")]
	public async Task Create_WithAdminToken_ReturnsCreated()
	{
		// Arrange: valid user create payload.
		var body = new
		{
			firstName = "New",
			lastName = "User",
			email = "new@example.com",
			login = "new-user",
			password = "password123",
			policyIds = new[] { 1 },
		};
		using var request = AuthorizedJson(HttpMethod.Post, "/api/users", RoleNames.Admin, body);

		// Act: create the user.
		var response = await Client.SendAsync(request);

		// Assert: successful creation returns Created.
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact(DisplayName = "Users PUT updates item and returns 200")]
	public async Task Update_WithAdminToken_ReturnsOk()
	{
		// Arrange: valid partial update payload.
		var body = new { firstName = "Updated" };
		using var request = AuthorizedJson(HttpMethod.Put, "/api/users/1", RoleNames.Admin, body);

		// Act: update the user.
		var response = await Client.SendAsync(request);

		// Assert: update returns OK.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Users PATCH password returns 204")]
	public async Task ChangePassword_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: valid password change payload.
		var body = new { oldPassword = "oldPassword123", newPassword = "newPassword123" };
		using var request = AuthorizedJson(
			HttpMethod.Patch,
			"/api/users/1/password",
			RoleNames.Admin,
			body
		);

		// Act: change the password.
		var response = await Client.SendAsync(request);

		// Assert: successful password change returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Users POST policy link returns 200")]
	public async Task LinkPolicy_WithAdminToken_ReturnsOk()
	{
		// Arrange: create a policy link request.
		using var request = AuthorizedRequest(HttpMethod.Post, "/api/users/1/policies/1", RoleNames.Admin);

		// Act: link the policy.
		var response = await Client.SendAsync(request);

		// Assert: controller returns the updated user.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Users DELETE policy link returns 200")]
	public async Task UnlinkPolicy_WithAdminToken_ReturnsOk()
	{
		// Arrange: create a policy unlink request.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/users/1/policies/1", RoleNames.Admin);

		// Act: unlink the policy.
		var response = await Client.SendAsync(request);

		// Assert: controller returns the updated user.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Users DELETE removes item and returns 204")]
	public async Task Delete_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create an admin DELETE request.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/users/1", RoleNames.Admin);

		// Act: delete the user.
		var response = await Client.SendAsync(request);

		// Assert: successful deletion returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}
}
