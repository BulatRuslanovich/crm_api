using System.Text.Json;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class GetPhysDetailsTool(IAssistantCrmReadPort crm) : IAssistantTool
{
	public string Name => "get_phys_details";

	public string Description =>
		"Get full details of a physician/contact by id, including specialty, contacts and linked organizations.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "id": { "type": "integer", "description": "Phys id from search_physes" }
		  },
		  "required": ["id"]
		}
		""";

	public async Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		if (!arguments.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
			return ToolExecutionResult.Error("Параметр 'id' обязателен");

		var result = await crm.GetPhysAsync(idEl.GetInt32());
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var p = result.Value!;
		return ToolExecutionResult.Ok(new
		{
			id = p.PhysId,
			firstName = p.FirstName,
			lastName = p.LastName,
			middleName = p.MiddleName,
			specialty = p.SpecName,
			phone = p.Phone,
			email = p.Email,
			orgs = p.Orgs.Select(o => new { id = o.OrgId, name = o.OrgName }),
		});
	}
}
