using System.Globalization;
using System.Text.Json;
using CrmWebApi.DTOs.Activ;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class ListActivsTool(IAssistantCrmReadPort crm) : IAssistantTool
{
	public string Name => "list_activs";

	public string Description =>
		"List activities (visits, meetings) visible to the current user. Filter by date range, statuses, free-text search. " +
		"The user's access scope is applied automatically - never pass user ids. " +
		"Dates must be ISO-8601 with timezone offset, e.g. 2026-05-10T00:00:00+03:00.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "from":     { "type": "string", "description": "Start of date range, ISO-8601 with offset" },
		    "to":       { "type": "string", "description": "End of date range, ISO-8601 with offset" },
		    "statuses": { "type": "array", "items": { "type": "integer" }, "description": "Optional status ids to filter" },
		    "search":   { "type": "string", "description": "Free-text search across description and related entities" },
		    "limit":    { "type": "integer", "minimum": 1, "maximum": 100, "default": 20 }
		  }
		}
		""";

	public async Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		var from = TryGetDate(arguments, "from");
		var to = TryGetDate(arguments, "to");
		var search = arguments.TryGetProperty("search", out var s) ? s.GetString() : null;
		var statuses = arguments.TryGetProperty("statuses", out var st) && st.ValueKind == JsonValueKind.Array
			? st.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt32()).ToArray()
			: null;
		var limit = arguments.TryGetProperty("limit", out var l) && l.ValueKind == JsonValueKind.Number
			? Math.Clamp(l.GetInt32(), 1, 100)
			: 20;

		var query = new ActivQuery(search, null, true, statuses, null)
		{
			Page = 1,
			PageSize = limit,
			DateFrom = from,
			DateTo = to,
			IncludeTotal = false,
		};

		var result = await crm.ListActivsAsync(query);
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var ru = CultureInfo.GetCultureInfo("ru-RU");
		var items = result.Value!.Items.Select(a => new
		{
			id = a.ActivId,
			when = FormatRange(a.Start, a.End, ru),
			status = a.StatusName,
			phys = a.PhysName,
			org = a.OrgName,
			description = a.Description,
		});

		return ToolExecutionResult.Ok(new { count = result.Value!.Items.Count(), items });
	}

	private static DateTimeOffset? TryGetDate(JsonElement args, string name)
	{
		if (!args.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String) return null;
		return DateTimeOffset.TryParse(el.GetString(), out var d) ? d : null;
	}

	private static string? FormatRange(DateTimeOffset? start, DateTimeOffset? end, CultureInfo ru)
	{
		if (start is null) return null;
		var s = start.Value.ToLocalTime().ToString("d MMMM, HH:mm", ru);
		if (end is null) return s;
		return $"{s} - {end.Value.ToLocalTime():HH:mm}";
	}
}
