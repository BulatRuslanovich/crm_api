using System.Net;

namespace CrmWebApi.Tests;

[Trait("Area", "Health")]
public sealed class HealthContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Health endpoint returns Healthy text response")]
	public async Task Health_ReturnsHealthy()
	{
		// Arrange: health checks are replaced with a deterministic healthy check in tests.
		using var request = new HttpRequestMessage(HttpMethod.Get, "/health");

		// Act: call the health endpoint.
		var response = await Client.SendAsync(request);

		// Assert: the health endpoint returns the ASP.NET Core health check text contract.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
	}
}
