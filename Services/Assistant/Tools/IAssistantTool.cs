using System.Text.Json;

namespace CrmWebApi.Services.Assistant.Tools;

public interface IAssistantTool
{
	string Name { get; }
	string Description { get; }
	string ParametersJsonSchema { get; }

	Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct);
}

public sealed record ToolExecutionResult(string ResultJson, bool IsError = false)
{
	public static ToolExecutionResult Ok(object payload) =>
		new(JsonSerializer.Serialize(payload), false);

	public static ToolExecutionResult Error(string message) =>
		new(JsonSerializer.Serialize(new { error = message }), true);
}
