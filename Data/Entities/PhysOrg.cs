using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("phys_org")]
public class PhysOrg
{
	[Column("phys_org_id")]
	public int PhysOrgId { get; set; }

	[Column("phys_id")]
	public int PhysId { get; set; }

	[Column("org_id")]
	public int OrgId { get; set; }

	public Phys Phys { get; set; } = null!;
	public Organization Org { get; set; } = null!;
}
