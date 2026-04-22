using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("phys")]
public class Phys
{
	[Column("phys_id")]
	public int PhysId { get; init; }

	[Column("spec_id")]
	public int SpecId { get; set; }

	[MaxLength(100)]
	[Column("phys_firstname")]
	public string PhysFirstname { get; set; } = string.Empty;

	[MaxLength(100)]
	[Column("phys_lastname")]
	public string PhysLastname { get; set; } = string.Empty;

	[MaxLength(100)]
	[Column("phys_middlename")]
	public string? PhysMiddlename { get; set; }

	[MaxLength(30)]
	[Column("phys_phone")]
	public string? PhysPhone { get; set; }

	[MaxLength(150)]
	[Column("phys_email")]
	public string PhysEmail { get; set; } = null!;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public Spec Spec { get; init; } = null!;
	public ICollection<PhysOrg> PhysOrgs { get; init; } = [];
}
