using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("usr_policy")]
public class UsrPolicy
{
	[Column("usr_id")]
	public int UsrId { get; set; }

	[Column("policy_id")]
	public int PolicyId { get; set; }

	public Usr Usr { get; set; } = null!;
	public Policy Policy { get; set; } = null!;
}
