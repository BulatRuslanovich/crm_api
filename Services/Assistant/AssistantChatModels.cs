namespace CrmWebApi.Services.Assistant;

public static class ChatRoles
{
	public const string System = "system";
	public const string User = "user";
	public const string Assistant = "assistant";
	public const string Tool = "tool";
}

public sealed record ChatHistoryMessage(
	string Role,
	string Content,
	IReadOnlyList<ChatToolCall>? ToolCalls = null,
	string? ToolCallId = null
);

public sealed record ChatToolCall(string Id, string Name, string ArgumentsJson);

public sealed record ToolDefinition(string Name, string Description, string ParametersJsonSchema);

public abstract record ChatProviderEvent;

public sealed record ChatTokenEvent(string Text) : ChatProviderEvent;

public sealed record ChatToolCallEvent(IReadOnlyList<ChatToolCall> Calls) : ChatProviderEvent;

public sealed record ChatFinishedEvent(string FullText, IReadOnlyList<ChatToolCall>? ToolCalls)
	: ChatProviderEvent;
