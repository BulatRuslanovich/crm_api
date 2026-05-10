using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CrmWebApi.DTOs.Assistant;
using CrmWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmWebApi.Controllers;

[Route("api/assistant")]
[Authorize]
public class AssistantController(IAssistantService service) : ApiController
{
	private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web)
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	[HttpPost("chat")]
	[EndpointSummary("Chat with the assistant (SSE stream)")]
	public async Task Chat([FromBody] ChatRequest request, CancellationToken ct)
	{
		var usrIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(usrIdClaim, out var usrId))
		{
			Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}

		Response.Headers.ContentType = "text/event-stream";
		Response.Headers.CacheControl = "no-cache";
		Response.Headers["X-Accel-Buffering"] = "no";

		await foreach (var ev in service.StreamAsync(usrId, request, ct))
		{
			var json = JsonSerializer.Serialize(ev, SseJsonOptions);
			var bytes = Encoding.UTF8.GetBytes($"data: {json}\n\n");
			await Response.Body.WriteAsync(bytes, ct);
			await Response.Body.FlushAsync(ct);
		}
	}

	[HttpGet("conversations")]
	[EndpointSummary("List my conversations")]
	[ProducesResponseType<IReadOnlyList<ConversationSummaryResponse>>(StatusCodes.Status200OK)]
	public async Task<IActionResult> List(CancellationToken ct)
	{
		var usrIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(usrIdClaim, out var usrId)) return ForbiddenProblem();
		return FromResult(await service.ListConversationsAsync(usrId, ct));
	}

	[HttpGet("conversations/{id:long}")]
	[EndpointSummary("Get conversation history")]
	[ProducesResponseType<ConversationDetailsResponse>(StatusCodes.Status200OK)]
	public async Task<IActionResult> Get(long id, CancellationToken ct)
	{
		var usrIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(usrIdClaim, out var usrId)) return ForbiddenProblem();
		return FromResult(await service.GetConversationAsync(usrId, id, ct));
	}

	[HttpDelete("conversations/{id:long}")]
	[EndpointSummary("Delete conversation")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> Delete(long id, CancellationToken ct)
	{
		var usrIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(usrIdClaim, out var usrId)) return ForbiddenProblem();
		return FromResult(await service.DeleteConversationAsync(usrId, id, ct));
	}

	[HttpPost("confirm/{actionId}")]
	[EndpointSummary("Execute a pending assistant action draft")]
	public async Task<IActionResult> Confirm(string actionId, CancellationToken ct)
	{
		var usrIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(usrIdClaim, out var usrId)) return ForbiddenProblem();
		return FromResult(await service.ConfirmActionAsync(usrId, actionId, ct));
	}
}
