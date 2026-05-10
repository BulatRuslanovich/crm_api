using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("assistant_conversation")]
public class AssistantConversation
{
	[Key]
	[Column("conversation_id")]
	public long ConversationId { get; init; }

	[Column("usr_id")]
	public int UsrId { get; init; }

	[MaxLength(255)]
	[Column("title")]
	public string? Title { get; set; }

	[Column("created_at")]
	public DateTimeOffset CreatedAt { get; init; }

	[Column("updated_at")]
	public DateTimeOffset UpdatedAt { get; set; }

	public ICollection<AssistantMessage> Messages { get; init; } = [];
}
