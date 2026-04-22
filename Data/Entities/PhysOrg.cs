using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("phys_org")]
public class PhysOrg
{
	[Column("phys_org_id")]
	public int PhysOrgId { get; init; }

	[Column("phys_id")]
	public int PhysId { get; init; }

	[Column("org_id")]
	public int OrgId { get; init; }

	public Phys Phys { get; init; } = null!;
	public Organization Org { get; init; } = null!;
}
