using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "Drugs")]
public sealed class DrugsControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Drugs GET list returns paged response")]
	public async Task GetAll_WithAdminToken_ReturnsPagedResponse()
	{
		// Arrange: an admin can access the drugs list.
		using var request = AuthorizedGet("/api/drugs?page=1&pageSize=2", RoleNames.Admin);

		// Act: request a paged list from the controller.
		var response = await Client.SendAsync(request);

		// Assert: the controller returns the standard PagedResponse shape.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("page").GetInt32());
		Assert.Equal(2, json.GetProperty("pageSize").GetInt32());
		Assert.Equal(3, json.GetProperty("totalCount").GetInt32());
		Assert.Equal(2, json.GetProperty("items").GetArrayLength());
	}

	[Fact(DisplayName = "Drugs GET by id returns item response")]
	public async Task GetById_WithAdminToken_ReturnsDrug()
	{
		// Arrange: request an existing fake drug id.
		using var request = AuthorizedGet("/api/drugs/1", RoleNames.Admin);

		// Act: request a drug by id.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the requested drug id.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("drugId").GetInt32());
	}

	[Fact(DisplayName = "Drugs POST creates item and returns 201")]
	public async Task Create_WithAdminToken_ReturnsCreated()
	{
		// Arrange: valid create payload for a drug.
		var body = new { drugName = "Цетиризин", brand = "Зиртек", form = "таблетки" };
		using var request = AuthorizedJson(HttpMethod.Post, "/api/drugs", RoleNames.Admin, body);

		// Act: create a drug.
		var response = await Client.SendAsync(request);

		// Assert: successful creation uses CreatedAtAction semantics.
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
		Assert.NotNull(response.Headers.Location);
	}

	[Fact(DisplayName = "Drugs PUT updates item and returns 200")]
	public async Task Update_WithAdminToken_ReturnsOk()
	{
		// Arrange: valid update payload for an existing drug.
		var body = new { drugName = "Ибупрофен", brand = "Нурофен", form = "капсулы" };
		using var request = AuthorizedJson(HttpMethod.Put, "/api/drugs/1", RoleNames.Admin, body);

		// Act: update the drug.
		var response = await Client.SendAsync(request);

		// Assert: update returns the updated resource contract.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Drugs DELETE removes item and returns 204")]
	public async Task Delete_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create an admin DELETE request.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/drugs/1", RoleNames.Admin);

		// Act: delete the drug.
		var response = await Client.SendAsync(request);

		// Assert: successful deletion has no response body.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}
}
