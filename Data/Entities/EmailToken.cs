using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("email_token")]
public class EmailToken
{
	[Column("email_token_id")]
	public long EmailTokenId { get; init; }

	[Column("usr_id")]
	public int UsrId { get; init; }

	[MaxLength(255)]
	[Column("token_hash")]
	public string TokenHash { get; init; } = null!;

	[Column("token_type")]
	public int TokenType { get; init; }

	[Column("expires_at")]
	public DateTime ExpiresAt { get; init; }

	[Column("attempt_count")]
	public int AttemptCount { get; set; }

	public Usr Usr { get; init; } = null!;
}
