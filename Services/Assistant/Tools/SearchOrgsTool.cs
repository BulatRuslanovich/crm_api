using System.Text.Json;
using CrmWebApi.Services;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class SearchOrgsTool(IOrgService orgService) : IAssistantTool
{
	public string Name => "search_orgs";

	public string Description =>
		"Search organizations by name, INN or address. Returns up to 'limit' matches.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "query": { "type": "string", "description": "Search text matched against organization name, INN, address" },
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

		var result = await orgService.GetAllAsync(1, limit, query, includeTotal: false);
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var items = result.Value!.Items.Select(o => new
		{
			id = o.OrgId,
			name = o.OrgName,
			type = o.OrgTypeName,
			inn = o.Inn,
			address = o.Address,
		});

		return ToolExecutionResult.Ok(new { items });
	}
}
