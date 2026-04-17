using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("refresh")]
public class Refresh
{
	[Column("refresh_id")]
	public long RefreshId { get; set; }

	[Column("usr_id")]
	public int UsrId { get; set; }

	[Column("refresh_token_hash")]
	public string RefreshTokenHash { get; set; } = string.Empty;

	[Column("refresh_expires_at")]
	public DateTime RefreshExpiresAt { get; set; }

	public Usr Usr { get; set; } = null!;
}
