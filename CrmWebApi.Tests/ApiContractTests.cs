using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CrmWebApi.Tests;

public class ApiContractTests(ApiTestFactory factory) : IClassFixture<ApiTestFactory>
{
	private readonly HttpClient client = factory.CreateClient();

	[Fact]
	public async Task ProtectedEndpoint_WithoutToken_ReturnsProblemDetails401()
	{
		var response = await client.GetAsync("/api/drugs");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

		var json = await response.Content.ReadFromJsonAsync<JsonElement>();
		Assert.Equal(401, json.GetProperty("status").GetInt32());
		Assert.Equal("Требуется авторизация", json.GetProperty("title").GetString());
		Assert.True(json.TryGetProperty("traceId", out _));
	}

	[Fact]
	public async Task AuthLogin_InvalidCredentials_ReturnsProblemDetails401()
	{
		var response = await client.PostAsJsonAsync(
			"/api/auth/login",
			new { login = "missing", password = "wrong" }
		);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

		var json = await response.Content.ReadFromJsonAsync<JsonElement>();
		Assert.Equal(401, json.GetProperty("status").GetInt32());
		Assert.Equal("Неверный логин или пароль", json.GetProperty("title").GetString());
	}

	[Fact]
	public async Task AuthRegister_InvalidBody_ReturnsValidationProblemDetails()
	{
		var response = await client.PostAsJsonAsync(
			"/api/auth/register",
			new { firstName = "", lastName = "", email = "not-email", login = "", password = "abc" }
		);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		var json = await response.Content.ReadFromJsonAsync<JsonElement>();
		Assert.Equal(400, json.GetProperty("status").GetInt32());
		Assert.True(json.GetProperty("errors").TryGetProperty("Email", out _));
		Assert.True(json.GetProperty("errors").TryGetProperty("Login", out _));
		Assert.True(json.GetProperty("errors").TryGetProperty("Password", out _));
	}

	[Fact]
	public async Task Health_ReturnsHealthy()
	{
		var response = await client.GetAsync("/health");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
	}
}
