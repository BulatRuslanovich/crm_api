using System.Net;
using System.Net.Http.Json;

namespace CrmWebApi.Tests;

[Trait("Controller", "Auth")]
public sealed class AuthControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Auth login returns 401 ProblemDetails for invalid credentials")]
	public async Task Login_InvalidCredentials_ReturnsProblemDetails401()
	{
		// Arrange: the fake auth service rejects every login attempt.
		var body = new { login = "missing", password = "wrong" };

		// Act: submit credentials to the public login endpoint.
		var response = await Client.PostAsJsonAsync("/api/auth/login", body);

		// Assert: invalid credentials are returned as a 401 ProblemDetails response.
		await AssertProblemDetailsAsync(
			response,
			HttpStatusCode.Unauthorized,
			"Неверный логин или пароль"
		);
	}

	[Fact(DisplayName = "Auth register returns validation ProblemDetails for invalid body")]
	public async Task Register_InvalidBody_ReturnsValidationProblemDetails()
	{
		// Arrange: send invalid values for fields covered by FluentValidation.
		var body = new
		{
			firstName = "",
			lastName = "",
			email = "not-email",
			login = "",
			password = "abc",
		};

		// Act: submit the invalid registration request.
		var response = await Client.PostAsJsonAsync("/api/auth/register", body);

		// Assert: validation errors must keep their field names in the response.
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

		var json = await ReadJsonAsync(response);
		var errors = json.GetProperty("errors");
		Assert.Equal(400, json.GetProperty("status").GetInt32());
		Assert.True(errors.TryGetProperty("Email", out _));
		Assert.True(errors.TryGetProperty("Login", out _));
		Assert.True(errors.TryGetProperty("Password", out _));
	}

	[Fact(DisplayName = "Auth register returns 202 Accepted for valid registration payload")]
	public async Task Register_ValidBody_ReturnsAccepted()
	{
		// Arrange: use a valid public registration payload.
		var body = new
		{
			firstName = "Test",
			lastName = "User",
			email = "test@example.com",
			login = "test",
			password = "password123",
		};

		// Act: submit the registration request.
		var response = await Client.PostAsJsonAsync("/api/auth/register", body);

		// Assert: registration keeps its pending-confirmation contract.
		Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
	}

	[Fact(DisplayName = "Auth resend-confirmation returns 204 for valid email body")]
	public async Task ResendConfirmation_ValidEmail_ReturnsNoContent()
	{
		// Arrange: provide the raw email string body expected by the controller.
		var email = "test@example.com";

		// Act: request a new confirmation code.
		var response = await Client.PostAsJsonAsync("/api/auth/resend-confirmation", email);

		// Assert: resend confirmation is intentionally silent.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Auth forgot-password returns 204 for valid email payload")]
	public async Task ForgotPassword_ValidBody_ReturnsNoContent()
	{
		// Arrange: provide a valid forgot-password payload.
		var body = new { email = "test@example.com" };

		// Act: request password reset.
		var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", body);

		// Assert: forgot password does not leak account existence.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Auth reset-password returns 204 for accepted reset payload")]
	public async Task ResetPassword_ValidBody_ReturnsNoContent()
	{
		// Arrange: provide the password reset payload shape expected by the controller.
		var body = new
		{
			email = "test@example.com",
			code = "123456",
			newPassword = "password123",
		};

		// Act: submit the reset password request.
		var response = await Client.PostAsJsonAsync("/api/auth/reset-password", body);

		// Assert: fake service accepts the request and controller returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Auth refresh returns 401 ProblemDetails without refresh cookie")]
	public async Task Refresh_WithoutCookie_ReturnsProblemDetails401()
	{
		// Arrange: do not send the refresh-token cookie.

		// Act: request token refresh.
		var response = await Client.PostAsync("/api/auth/refresh", content: null);

		// Assert: missing cookie is mapped to shared Unauthorized ProblemDetails.
		await AssertProblemDetailsAsync(
			response,
			HttpStatusCode.Unauthorized,
			"Refresh token отсутствует"
		);
	}

	[Fact(DisplayName = "Auth logout returns 204 without refresh cookie")]
	public async Task Logout_WithoutCookie_ReturnsNoContent()
	{
		// Arrange: logout is intentionally idempotent when no cookie exists.

		// Act: call logout without a refresh-token cookie.
		var response = await Client.PostAsync("/api/auth/logout", content: null);

		// Assert: logout completes successfully and clears the cookie.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}
}
