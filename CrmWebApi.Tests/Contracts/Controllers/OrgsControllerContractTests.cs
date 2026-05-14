using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "Organizations")]
public sealed class OrgsControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Organizations GET list returns paged response")]
	public async Task GetAll_WithAdminToken_ReturnsPagedResponse()
	{
		// Arrange: request the organizations list as an authenticated user.
		using var request = AuthorizedGet("/api/orgs", RoleNames.Admin);

		// Act: call the list endpoint.
		var response = await Client.SendAsync(request);

		// Assert: the endpoint returns a standard paged response.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
	}

	[Fact(DisplayName = "Organizations GET by id returns item response")]
	public async Task GetById_WithAdminToken_ReturnsOrg()
	{
		// Arrange: request an existing fake organization id.
		using var request = AuthorizedGet("/api/orgs/1", RoleNames.Admin);

		// Act: call the item endpoint.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the requested organization.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("orgId").GetInt32());
	}

	[Fact(DisplayName = "Organizations GET types returns reference data")]
	public async Task GetTypes_WithAdminToken_ReturnsReferenceData()
	{
		// Arrange: organization types are protected reference data.
		using var request = AuthorizedGet("/api/orgs/types", RoleNames.Admin);

		// Act: request organization types.
		var response = await Client.SendAsync(request);

		// Assert: reference endpoints return a JSON array.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetArrayLength());
		Assert.Equal("Аптека", json[0].GetProperty("orgTypeName").GetString());
	}

	[Fact(DisplayName = "Organizations POST creates item and returns 201")]
	public async Task Create_WithAdminToken_ReturnsCreated()
	{
		// Arrange: valid create payload for an organization.
		var body = new
		{
			orgTypeId = 1,
			orgName = "Новая аптека",
			inn = "7701000002",
			latitude = 55.75,
			longitude = 37.61,
			address = "Москва",
		};
		using var request = AuthorizedJson(HttpMethod.Post, "/api/orgs", RoleNames.Admin, body);

		// Act: create the organization.
		var response = await Client.SendAsync(request);

		// Assert: successful creation returns Created.
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact(DisplayName = "Organizations PUT updates item and returns 200")]
	public async Task Update_WithAdminToken_ReturnsOk()
	{
		// Arrange: valid partial update payload.
		var body = new { orgName = "Аптека N1" };
		using var request = AuthorizedJson(HttpMethod.Put, "/api/orgs/1", RoleNames.Admin, body);

		// Act: update the organization.
		var response = await Client.SendAsync(request);

		// Assert: update returns OK with item payload.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Organizations DELETE removes item and returns 204")]
	public async Task Delete_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create an admin DELETE request.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/orgs/1", RoleNames.Admin);

		// Act: delete the organization.
		var response = await Client.SendAsync(request);

		// Assert: successful deletion returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Organizations GET by id returns 404 for missing item")]
	public async Task GetById_ReturnsNotFound_WhenOrgDoesNotExist()
	{
		// Arrange: request a non-existent organization id.
		using var request = AuthorizedGet("/api/orgs/999", RoleNames.Admin);

		// Act: call the item endpoint with an unknown id.
		var response = await Client.SendAsync(request);

		// Assert: missing organization maps to 404 ProblemDetails.
		await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound, "Организация 999 не найдена");
	}

	[Fact(DisplayName = "Organizations POST returns 403 for non-admin role")]
	public async Task Create_WithRepresentativeToken_ReturnsForbidden()
	{
		// Arrange: only admins can create organizations.
		using var request = AuthorizedRequest(HttpMethod.Post, "/api/orgs", RoleNames.Representative);

		// Act: attempt to create an organization without the required role.
		var response = await Client.SendAsync(request);

		// Assert: role failure uses the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}

	[Fact(DisplayName = "Organizations PUT returns 403 for non-admin role")]
	public async Task Update_WithRepresentativeToken_ReturnsForbidden()
	{
		// Arrange: only admins can update organizations.
		using var request = AuthorizedRequest(HttpMethod.Put, "/api/orgs/1", RoleNames.Representative);

		// Act: attempt to update an organization without the required role.
		var response = await Client.SendAsync(request);

		// Assert: role failure uses the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}

	[Fact(DisplayName = "Organizations DELETE returns 403 for non-admin role")]
	public async Task Delete_WithRepresentativeToken_ReturnsForbidden()
	{
		// Arrange: only admins can delete organizations.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/orgs/1", RoleNames.Representative);

		// Act: attempt to delete an organization without the required role.
		var response = await Client.SendAsync(request);

		// Assert: role failure uses the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}
}
