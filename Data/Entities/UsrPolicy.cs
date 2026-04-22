using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("usr_policy")]
public class UsrPolicy
{
	[Column("usr_id")]
	public int UsrId { get; init; }

	[Column("policy_id")]
	public int PolicyId { get; init; }

	public Usr Usr { get; init; } = null!;
	public Policy Policy { get; init; } = null!;
}
