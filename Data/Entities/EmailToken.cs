using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("email_token")]
public class EmailToken
{
	[Column("email_token_id")]
	public long EmailTokenId { get; set; }

	[Column("usr_id")]
	public int UsrId { get; set; }

	[Column("token_hash")]
	public string TokenHash { get; set; } = null!;

	[Column("token_type")]
	public int TokenType { get; set; }

	[Column("expires_at")]
	public DateTime ExpiresAt { get; set; }

	[Column("attempt_count")]
	public int AttemptCount { get; set; }

	public Usr Usr { get; set; } = null!;
}
