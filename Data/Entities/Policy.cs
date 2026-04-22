using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("policy")]
public class Policy
{
	[Column("policy_id")]
	public int PolicyId { get; init; }

	[MaxLength(100)]
	[Column("policy_name")]
	public string PolicyName { get; init; } = null!;

	public ICollection<UsrPolicy> UsrPolicies { get; init; } = [];
}
