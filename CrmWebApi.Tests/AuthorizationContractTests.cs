using System.Net;

namespace CrmWebApi.Tests;

[Trait("Area", "Authorization")]
public sealed class AuthorizationContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Theory(DisplayName = "Protected GET endpoints return 401 ProblemDetails without JWT")]
	[InlineData("/api/activs")]
	[InlineData("/api/departments")]
	[InlineData("/api/drugs")]
	[InlineData("/api/orgs")]
	[InlineData("/api/physes")]
	[InlineData("/api/users")]
	public async Task ProtectedGetEndpoint_WithoutToken_ReturnsProblemDetails401(string url)
	{
		// Arrange: call a protected endpoint without an Authorization header.
		using var request = new HttpRequestMessage(HttpMethod.Get, url);

		// Act: execute the request through the full ASP.NET Core pipeline.
		var response = await Client.SendAsync(request);

		// Assert: unauthenticated requests must use the shared ProblemDetails contract.
		var json = await AssertProblemDetailsAsync(
			response,
			HttpStatusCode.Unauthorized,
			"Требуется авторизация"
		);

		Assert.True(json.TryGetProperty("traceId", out _));
	}
}
