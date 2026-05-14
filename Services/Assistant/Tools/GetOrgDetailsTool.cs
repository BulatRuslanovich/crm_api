using System.Text.Json;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class GetOrgDetailsTool(IAssistantCrmReadPort crm) : IAssistantTool
{
	public string Name => "get_org_details";

	public string Description =>
		"Get full details of an organization by id (type, INN, address, coordinates).";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "id": { "type": "integer", "description": "Organization id from search_orgs" }
		  },
		  "required": ["id"]
		}
		""";

	public async Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		if (!arguments.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
			return ToolExecutionResult.Error("Параметр 'id' обязателен");

		var result = await crm.GetOrgAsync(idEl.GetInt32());
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var o = result.Value!;
		return ToolExecutionResult.Ok(new
		{
			id = o.OrgId,
			name = o.OrgName,
			type = o.OrgTypeName,
			inn = o.Inn,
			address = o.Address,
			latitude = o.Latitude,
			longitude = o.Longitude,
		});
	}
}
