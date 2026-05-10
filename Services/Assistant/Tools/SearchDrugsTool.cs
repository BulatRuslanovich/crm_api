using System.Text.Json;
using CrmWebApi.Services;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class SearchDrugsTool(IDrugService drugService) : IAssistantTool
{
	public string Name => "search_drugs";

	public string Description =>
		"Search drugs by name or brand. Returns up to 'limit' matches with id, name, brand and form.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "query": { "type": "string", "description": "Search text matched against drug name and brand" },
		    "limit": { "type": "integer", "minimum": 1, "maximum": 50, "default": 10 }
		  },
		  "required": ["query"]
		}
		""";

	public async Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		var query = arguments.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;
		var limit = arguments.TryGetProperty("limit", out var l) && l.ValueKind == JsonValueKind.Number
			? Math.Clamp(l.GetInt32(), 1, 50)
			: 10;

		var result = await drugService.GetAllAsync(1, limit, query, includeTotal: false);
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var items = result.Value!.Items.Select(d => new
		{
			id = d.DrugId,
			name = d.DrugName,
			brand = d.Brand,
			form = d.Form,
		});

		return ToolExecutionResult.Ok(new { items });
	}
}
