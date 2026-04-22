using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("refresh")]
public class Refresh
{
	[Column("refresh_id")]
	public long RefreshId { get; init; }

	[Column("usr_id")]
	public int UsrId { get; init; }

	[MaxLength(255)]
	[Column("refresh_token_hash")]
	public string RefreshTokenHash { get; init; } = string.Empty;

	[Column("refresh_expires_at")]
	public DateTime RefreshExpiresAt { get; init; }

	public Usr Usr { get; init; } = null!;
}
