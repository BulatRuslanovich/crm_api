using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("org")]
public class Organization
{
	[Key]
	[Column("org_id")]
	public int OrgId { get; set; }

	[Column("org_type_id")]
	public int OrgTypeId { get; set; }

	[Column("org_name")]
	public string OrgName { get; set; } = null!;

	[Column("org_inn")]
	public string OrgInn { get; set; } = null!;

	[Column("org_latitude")]
	public double OrgLatitude { get; set; } = 0;

	[Column("org_longitude")]
	public double OrgLongitude { get; set; } = 0;

	[Column("org_address")]
	public string OrgAddress { get; set; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public OrgType OrgType { get; set; } = null!;
	public ICollection<PhysOrg> PhysOrgs { get; set; } = [];
	public ICollection<Activ> Activs { get; set; } = [];
}
