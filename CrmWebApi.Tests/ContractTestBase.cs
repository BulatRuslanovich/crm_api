using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace CrmWebApi.Tests;

public abstract class ContractTestBase(ApiTestFactory factory)
{
	private const string JwtIssuer = "PharmaCrmApi";
	private const string JwtAudience = "PharmaCrmClient";
	private const string JwtSecret = "integration-test-secret-with-more-than-32-chars";

	protected HttpClient Client { get; } = factory.CreateClient();

	protected static HttpRequestMessage AuthorizedGet(
		string url,
		string role,
		int userId = 1
	) => AuthorizedRequest(HttpMethod.Get, url, role, userId);

	protected static HttpRequestMessage AuthorizedJson<T>(
		HttpMethod method,
		string url,
		string role,
		T body,
		int userId = 1
	)
	{
		var request = AuthorizedRequest(method, url, role, userId);
		request.Content = JsonContent.Create(body);
		return request;
	}

	protected static HttpRequestMessage AuthorizedRequest(
		HttpMethod method,
		string url,
		string role,
		int userId = 1
	)
	{
		var request = new HttpRequestMessage(method, url);
		request.Headers.Authorization = new AuthenticationHeaderValue(
			"Bearer",
			CreateJwt(userId, role)
		);

		return request;
	}

	protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
	{
		var json = await response.Content.ReadFromJsonAsync<JsonElement>();
		return json;
	}

	protected static void AssertProblemDetailsContent(HttpResponseMessage response) =>
		Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

	protected static async Task<JsonElement> AssertProblemDetailsAsync(
		HttpResponseMessage response,
		HttpStatusCode statusCode,
		string title
	)
	{
		Assert.Equal(statusCode, response.StatusCode);
		AssertProblemDetailsContent(response);

		var json = await ReadJsonAsync(response);
		Assert.Equal((int)statusCode, json.GetProperty("status").GetInt32());
		Assert.Equal(title, json.GetProperty("title").GetString());
		return json;
	}

	private static string CreateJwt(int userId, string role)
	{
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
			new Claim(ClaimTypes.Role, role),
		};
		var token = new JwtSecurityToken(
			issuer: JwtIssuer,
			audience: JwtAudience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(10),
			signingCredentials: credentials
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
