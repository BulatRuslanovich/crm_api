using System.Text.Encodings.Web;
using System.Text.Json;

namespace CrmWebApi.Services.Assistant.Tools;

public interface IAssistantTool
{
	public string Name { get; }
	public string Description { get; }
	public string ParametersJsonSchema { get; }

	public Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct);
}

public sealed record ToolExecutionResult(string ResultJson, bool IsError = false)
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
	{
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};

	public static ToolExecutionResult Ok(object payload) =>
		new(JsonSerializer.Serialize(payload, JsonOptions), false);

	public static ToolExecutionResult Error(string message) =>
		new(JsonSerializer.Serialize(new { error = message }, JsonOptions), true);
}
