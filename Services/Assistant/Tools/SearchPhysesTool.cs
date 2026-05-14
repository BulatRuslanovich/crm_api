using System.Text.Json;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class SearchPhysesTool(IAssistantCrmReadPort crm) : IAssistantTool
{
	public string Name => "search_physes";

	public string Description =>
		"Search physicians/contacts by name, phone or email. Returns up to 'limit' matches with id, name and specialty.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "query": { "type": "string", "description": "Search text matched against names, phone and email" },
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

		var result = await crm.SearchPhysesAsync(query, limit);
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var items = result.Value!.Items.Select(p => new
		{
			id = p.PhysId,
			firstName = p.FirstName,
			lastName = p.LastName,
			middleName = p.MiddleName,
			specialty = p.SpecName,
			email = p.Email,
			orgs = p.Orgs.Select(o => new { id = o.OrgId, name = o.OrgName }),
		});

		return ToolExecutionResult.Ok(new { items });
	}
}
