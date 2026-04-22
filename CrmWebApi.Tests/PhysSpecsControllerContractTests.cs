using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "ContactSpecialties")]
public sealed class PhysSpecsControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Contact specialties GET list returns reference data")]
	public async Task GetAllSpecs_WithAdminToken_ReturnsReferenceData()
	{
		// Arrange: specialties are protected reference data.
		using var request = AuthorizedGet("/api/physes/specs", RoleNames.Admin);

		// Act: request specialties.
		var response = await Client.SendAsync(request);

		// Assert: reference endpoints return a JSON array.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetArrayLength());
		Assert.Equal("Терапевт", json[0].GetProperty("specName").GetString());
	}

	[Fact(DisplayName = "Contact specialties GET by id returns item response")]
	public async Task GetSpecById_WithAdminToken_ReturnsSpec()
	{
		// Arrange: request an existing specialty id.
		using var request = AuthorizedGet("/api/physes/specs/1", RoleNames.Admin);

		// Act: request the specialty.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the requested specialty id.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("specId").GetInt32());
	}

	[Fact(DisplayName = "Contact specialties POST creates item and returns 201")]
	public async Task CreateSpec_WithAdminToken_ReturnsCreated()
	{
		// Arrange: valid specialty create payload.
		var body = new { specName = "Кардиолог" };
		using var request = AuthorizedJson(HttpMethod.Post, "/api/physes/specs", RoleNames.Admin, body);

		// Act: create the specialty.
		var response = await Client.SendAsync(request);

		// Assert: successful creation returns Created.
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact(DisplayName = "Contact specialties DELETE removes item and returns 204")]
	public async Task DeleteSpec_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create an admin DELETE request.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/physes/specs/1", RoleNames.Admin);

		// Act: delete the specialty.
		var response = await Client.SendAsync(request);

		// Assert: successful deletion returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}
}
