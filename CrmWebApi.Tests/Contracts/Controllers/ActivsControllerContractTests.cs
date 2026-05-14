using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "Activities")]
public sealed class ActivsControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Activities GET list returns scoped paged response")]
	public async Task GetAll_WithRepresentativeToken_ReturnsScopedPagedResponse()
	{
		// Arrange: representatives can list only their scoped activities.
		using var request = AuthorizedGet("/api/activs?page=1&pageSize=10", RoleNames.Representative);

		// Act: request activities through scope-aware controller logic.
		var response = await Client.SendAsync(request);

		// Assert: a valid token with a user id produces a successful scoped response.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("page").GetInt32());
		Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
		Assert.Equal("Визит", json.GetProperty("items")[0].GetProperty("description").GetString());
	}

	[Fact(DisplayName = "Activities GET by id returns scoped item response")]
	public async Task GetById_WithRepresentativeToken_ReturnsActiv()
	{
		// Arrange: request an existing scoped activity.
		using var request = AuthorizedGet("/api/activs/1", RoleNames.Representative);

		// Act: call the item endpoint.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the activity id.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("activId").GetInt32());
	}

	[Fact(DisplayName = "Activities POST creates item and returns 201")]
	public async Task Create_WithRepresentativeToken_ReturnsCreated()
	{
		// Arrange: valid create payload with exactly one target type.
		var body = new
		{
			orgId = 1,
			physId = (int?)null,
			statusId = 1,
			start = DateTimeOffset.UtcNow,
			end = DateTimeOffset.UtcNow.AddHours(1),
			description = "Визит",
			drugIds = new[] { 1 },
		};
		using var request = AuthorizedJson(
			HttpMethod.Post,
			"/api/activs",
			RoleNames.Representative,
			body
		);

		// Act: create the activity.
		var response = await Client.SendAsync(request);

		// Assert: successful creation returns Created.
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact(DisplayName = "Activities PUT updates item and returns 200")]
	public async Task Update_WithRepresentativeToken_ReturnsOk()
	{
		// Arrange: valid activity update payload.
		var body = new { description = "Обновленный визит" };
		using var request = AuthorizedJson(
			HttpMethod.Put,
			"/api/activs/1",
			RoleNames.Representative,
			body
		);

		// Act: update the activity.
		var response = await Client.SendAsync(request);

		// Assert: update returns OK.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Activities POST drug link returns 204")]
	public async Task LinkDrug_WithRepresentativeToken_ReturnsNoContent()
	{
		// Arrange: create a link request between activity and drug.
		using var request = AuthorizedRequest(
			HttpMethod.Post,
			"/api/activs/1/drugs/1",
			RoleNames.Representative
		);

		// Act: link the drug.
		var response = await Client.SendAsync(request);

		// Assert: successful link returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Activities DELETE drug link returns 204")]
	public async Task UnlinkDrug_WithRepresentativeToken_ReturnsNoContent()
	{
		// Arrange: create an unlink request between activity and drug.
		using var request = AuthorizedRequest(
			HttpMethod.Delete,
			"/api/activs/1/drugs/1",
			RoleNames.Representative
		);

		// Act: unlink the drug.
		var response = await Client.SendAsync(request);

		// Assert: successful unlink returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Activities DELETE removes item and returns 204")]
	public async Task Delete_WithRepresentativeToken_ReturnsNoContent()
	{
		// Arrange: create a scoped DELETE request.
		using var request = AuthorizedRequest(
			HttpMethod.Delete,
			"/api/activs/1",
			RoleNames.Representative
		);

		// Act: delete the activity.
		var response = await Client.SendAsync(request);

		// Assert: successful deletion returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Activities GET by id returns 404 for missing item")]
	public async Task GetById_ReturnsNotFound_WhenActivDoesNotExist()
	{
		// Arrange: request a non-existent activity id.
		using var request = AuthorizedGet("/api/activs/999", RoleNames.Representative);

		// Act: call the item endpoint with an unknown id.
		var response = await Client.SendAsync(request);

		// Assert: missing activity maps to 404 ProblemDetails.
		await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound, "Активность 999 не найдена");
	}
}
