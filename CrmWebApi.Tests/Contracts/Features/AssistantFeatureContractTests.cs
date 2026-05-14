using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "Assistant")]
public sealed class AssistantFeatureContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Assistant returns 503 when feature is disabled")]
	public async Task ListConversations_ReturnsServiceUnavailable_WhenAssistantDisabled()
	{
		using var request = AuthorizedGet("/api/assistant/conversations", RoleNames.Representative);

		var response = await Client.SendAsync(request);

		await AssertProblemDetailsAsync(response, HttpStatusCode.ServiceUnavailable, "AI assistant is disabled.");
	}
}
