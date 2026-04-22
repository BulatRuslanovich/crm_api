using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "Contacts")]
public sealed class PhysesControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Contacts GET list returns paged response")]
	public async Task GetAll_WithAdminToken_ReturnsPagedResponse()
	{
		// Arrange: request the contacts list.
		using var request = AuthorizedGet("/api/physes", RoleNames.Admin);

		// Act: call the list endpoint.
		var response = await Client.SendAsync(request);

		// Assert: the endpoint returns a standard paged response.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Contacts GET by id returns item response")]
	public async Task GetById_WithAdminToken_ReturnsPhys()
	{
		// Arrange: request an existing fake contact id.
		using var request = AuthorizedGet("/api/physes/1", RoleNames.Admin);

		// Act: call the item endpoint.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the requested contact.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("physId").GetInt32());
	}

	[Fact(DisplayName = "Contacts POST creates item and returns 201")]
	public async Task Create_WithAdminToken_ReturnsCreated()
	{
		// Arrange: valid create payload for a contact.
		var body = new
		{
			specId = 1,
			firstName = "Петр",
			lastName = "Петров",
			middleName = "Петрович",
			phone = "+70000000001",
			email = "petrov@example.com",
		};
		using var request = AuthorizedJson(HttpMethod.Post, "/api/physes", RoleNames.Admin, body);

		// Act: create the contact.
		var response = await Client.SendAsync(request);

		// Assert: successful creation returns Created.
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact(DisplayName = "Contacts PUT updates item and returns 200")]
	public async Task Update_WithAdminToken_ReturnsOk()
	{
		// Arrange: valid partial update payload.
		var body = new { firstName = "Иван" };
		using var request = AuthorizedJson(HttpMethod.Put, "/api/physes/1", RoleNames.Admin, body);

		// Act: update the contact.
		var response = await Client.SendAsync(request);

		// Assert: update returns OK.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Contacts POST organization link returns 204")]
	public async Task LinkOrg_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create a link request between contact and organization.
		using var request = AuthorizedRequest(HttpMethod.Post, "/api/physes/1/orgs/1", RoleNames.Admin);

		// Act: link the organization.
		var response = await Client.SendAsync(request);

		// Assert: successful link returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Contacts DELETE organization link returns 204")]
	public async Task UnlinkOrg_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create an unlink request between contact and organization.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/physes/1/orgs/1", RoleNames.Admin);

		// Act: unlink the organization.
		var response = await Client.SendAsync(request);

		// Assert: successful unlink returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Contacts DELETE removes item and returns 204")]
	public async Task Delete_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create an admin DELETE request.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/physes/1", RoleNames.Admin);

		// Act: delete the contact.
		var response = await Client.SendAsync(request);

		// Assert: successful deletion returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}
}
