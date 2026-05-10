using System.Globalization;
using System.Text.Json;
using CrmWebApi.DTOs.Activ;
using CrmWebApi.Services;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class ProposeCreateActivTool(
	ICurrentUserService currentUser,
	IAssistantActionStore actionStore
) : IAssistantTool
{
	public string Name => "propose_create_activ";

	public string Description =>
		"Prepare a draft of a new activity (visit) for the user to confirm. " +
		"DOES NOT write to the database — only stores a pending draft. " +
		"Always run search_physes / search_orgs first to resolve ids; do not invent ids. " +
		"After calling this tool, briefly summarize the draft and ask the user to confirm. Do not mention UI elements (buttons, dialogs). " +
		"Statuses: 1=Запланирован, 2=Открыт, 3=Сохранён, 4=Закрыт, 5=Отменён. Use 1 for new visits unless user said otherwise.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "physId":      { "type": "integer", "description": "Physician id from search_physes (optional if visit not tied to a phys)" },
		    "orgId":       { "type": "integer", "description": "Organization id from search_orgs (optional)" },
		    "statusId":    { "type": "integer", "description": "Status id, default 1 (Запланирован)" },
		    "start":       { "type": "string",  "description": "ISO-8601 with offset, e.g. 2026-05-11T10:00:00+03:00" },
		    "end":         { "type": "string",  "description": "Optional end time, ISO-8601 with offset" },
		    "description": { "type": "string",  "description": "Free-text note about the visit" },
		    "latitude":    { "type": "number" },
		    "longitude":   { "type": "number" }
		  },
		  "required": ["statusId", "start", "description"]
		}
		""";

	public Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		if (currentUser.UsrId is not { } usrId)
			return Task.FromResult(ToolExecutionResult.Error("Не удалось определить пользователя"));

		if (!arguments.TryGetProperty("statusId", out var statusEl) || statusEl.ValueKind != JsonValueKind.Number)
			return Task.FromResult(ToolExecutionResult.Error("statusId обязателен"));
		if (!arguments.TryGetProperty("description", out var descEl) || descEl.ValueKind != JsonValueKind.String)
			return Task.FromResult(ToolExecutionResult.Error("description обязателен"));
		if (!arguments.TryGetProperty("start", out var startEl) || startEl.ValueKind != JsonValueKind.String
			|| !DateTimeOffset.TryParse(startEl.GetString(), out var start))
			return Task.FromResult(ToolExecutionResult.Error("start должен быть ISO-8601 с offset"));

		DateTimeOffset? end = null;
		if (arguments.TryGetProperty("end", out var endEl) && endEl.ValueKind == JsonValueKind.String
			&& DateTimeOffset.TryParse(endEl.GetString(), out var endParsed))
			end = endParsed;

		var req = new CreateActivRequest(
			OrgId: arguments.TryGetProperty("orgId", out var orgEl) && orgEl.ValueKind == JsonValueKind.Number
				? orgEl.GetInt32() : null,
			PhysId: arguments.TryGetProperty("physId", out var physEl) && physEl.ValueKind == JsonValueKind.Number
				? physEl.GetInt32() : null,
			StatusId: statusEl.GetInt32(),
			Start: start,
			End: end,
			Description: descEl.GetString()!,
			Latitude: arguments.TryGetProperty("latitude", out var latEl) && latEl.ValueKind == JsonValueKind.Number
				? latEl.GetDouble() : null,
			Longitude: arguments.TryGetProperty("longitude", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number
				? lonEl.GetDouble() : null
		);

		var ru = CultureInfo.GetCultureInfo("ru-RU");
		var summary = BuildSummary(req, ru);

		var payloadJson = JsonSerializer.Serialize(req);
		var action = actionStore.Put(usrId, Name, payloadJson, summary);

		return Task.FromResult(ToolExecutionResult.Ok(new
		{
			actionId = action.Id,
			summary,
			expiresInMinutes = (int)(action.ExpiresAt - DateTimeOffset.UtcNow).TotalMinutes,
			next = "Скажи пользователю что черновик готов и попроси подтвердить создание. Не упоминай кнопки/интерфейс.",
		}));
	}

	private static string BuildSummary(CreateActivRequest req, CultureInfo ru)
	{
		var when = req.Start.ToLocalTime().ToString("d MMMM yyyy, HH:mm", ru);
		if (req.End is { } endVal)
			when += $" – {endVal.ToLocalTime():HH:mm}";

		var parts = new List<string> { $"Визит {when}" };
		if (req.PhysId is { } pid) parts.Add($"к врачу #{pid}");
		if (req.OrgId is { } oid) parts.Add($"в организацию #{oid}");
		parts.Add($"статус {StatusName(req.StatusId)}");
		if (!string.IsNullOrWhiteSpace(req.Description)) parts.Add($"заметка: «{req.Description}»");

		return string.Join(", ", parts);
	}

	private static string StatusName(int id) => id switch
	{
		1 => "Запланирован",
		2 => "Открыт",
		3 => "Сохранён",
		4 => "Закрыт",
		5 => "Отменён",
		_ => $"#{id}",
	};
}
