namespace CrmWebApi.DTOs.Assistant;

public sealed record ChatStreamEvent(string Type, object? Data = null);

public static class ChatStreamEventType
{
	public const string ConversationStarted = "conversation_started";
	public const string Token = "token";
	public const string ToolCall = "tool_call";
	public const string ToolResult = "tool_result";
	public const string Done = "done";
	public const string Error = "error";
}

public sealed record ConversationStartedPayload(long ConversationId);

public sealed record TokenPayload(string Text);

public sealed record ToolCallPayload(string Name, string ArgumentsJson);

public sealed record ToolResultPayload(string Name, string ResultJson, bool IsError);

public sealed record ErrorPayload(string Message);
