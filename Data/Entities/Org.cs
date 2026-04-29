using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("org")]
public class Organization
{
	[Key]
	[Column("org_id")]
	public int OrgId { get; init; }

	[Column("org_type_id")]
	public int OrgTypeId { get; set; }

	[MaxLength(255)]
	[Column("org_name")]
	public string OrgName { get; set; } = null!;

	[MaxLength(12)]
	[Column("org_inn")]
	public string OrgInn { get; set; } = null!;

	[Column("org_latitude")]
	public double? OrgLatitude { get; set; }

	[Column("org_longitude")]
	public double? OrgLongitude { get; set; }

	[MaxLength(500)]
	[Column("org_address")]
	public string OrgAddress { get; set; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public OrgType OrgType { get; init; } = null!;
	public ICollection<PhysOrg> PhysOrgs { get; init; } = [];
	public ICollection<Activ> Activs { get; init; } = [];
}
