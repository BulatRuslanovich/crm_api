using System.Text.Json;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class GetDrugDetailsTool(IAssistantCrmReadPort crm) : IAssistantTool
{
	public string Name => "get_drug_details";

	public string Description =>
		"Get full details of a single drug by its numeric id. Use after search_drugs when the user wants more info about a specific item.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "id": { "type": "integer", "description": "Drug id from search_drugs" }
		  },
		  "required": ["id"]
		}
		""";

	public async Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		if (!arguments.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
			return ToolExecutionResult.Error("Параметр 'id' обязателен");

		var result = await crm.GetDrugAsync(idEl.GetInt32());
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var d = result.Value!;
		return ToolExecutionResult.Ok(new
		{
			id = d.DrugId,
			name = d.DrugName,
			brand = d.Brand,
			form = d.Form,
		});
	}
}
