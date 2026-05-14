using System.Globalization;
using System.Text.Json;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed class GetActivDetailsTool(IAssistantCrmReadPort crm) : IAssistantTool
{
	public string Name => "get_activ_details";

	public string Description =>
		"Get full details of an activity (visit) by id, including linked drugs. Respects current user's access scope.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "id": { "type": "integer", "description": "Activity id from list_activs" }
		  },
		  "required": ["id"]
		}
		""";

	public async Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		if (!arguments.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
			return ToolExecutionResult.Error("Параметр 'id' обязателен");

		var result = await crm.GetActivAsync(idEl.GetInt32());
		if (!result.IsSuccess)
			return ToolExecutionResult.Error(result.Error!.Message);

		var a = result.Value!;
		var ru = CultureInfo.GetCultureInfo("ru-RU");
		return ToolExecutionResult.Ok(new
		{
			id = a.ActivId,
			when = FormatRange(a.Start, a.End, ru),
			status = a.StatusName,
			phys = a.PhysId is null ? null : new { id = a.PhysId, name = a.PhysName },
			org = a.OrgId is null ? null : new { id = a.OrgId, name = a.OrgName },
			description = a.Description,
			drugs = a.Drugs.Select(d => new { id = d.DrugId, name = d.DrugName, brand = d.Brand }),
		});
	}

	private static string? FormatRange(DateTimeOffset? start, DateTimeOffset? end, CultureInfo ru)
	{
		if (start is null) return null;
		var s = start.Value.ToLocalTime().ToString("d MMMM yyyy, HH:mm", ru);
		if (end is null) return s;
		return $"{s} - {end.Value.ToLocalTime():HH:mm}";
	}
}
