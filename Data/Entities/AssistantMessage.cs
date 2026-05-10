using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("assistant_message")]
public class AssistantMessage
{
	[Key]
	[Column("message_id")]
	public long MessageId { get; init; }

	[Column("conversation_id")]
	public long ConversationId { get; init; }

	[MaxLength(20)]
	[Column("role")]
	public string Role { get; init; } = null!;

	[Column("content")]
	public string Content { get; init; } = null!;

	[Column("tool_calls")]
	public string? ToolCalls { get; init; }

	[Column("tool_call_id")]
	[MaxLength(128)]
	public string? ToolCallId { get; init; }

	[MaxLength(64)]
	[Column("provider")]
	public string? Provider { get; init; }

	[Column("created_at")]
	public DateTimeOffset CreatedAt { get; init; }

	public AssistantConversation? Conversation { get; init; }
}
