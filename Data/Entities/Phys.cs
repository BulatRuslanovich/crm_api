using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("phys")]
public class Phys
{
	[Column("phys_id")]
	public int PhysId { get; set; }

	[Column("spec_id")]
	public int SpecId { get; set; }

	[Column("phys_firstname")]
	public string PhysFirstname { get; set; } = string.Empty;

	[Column("phys_lastname")]
	public string PhysLastname { get; set; } = string.Empty;

	[Column("phys_middlename")]
	public string? PhysMiddlename { get; set; }

	[Column("phys_phone")]
	public string? PhysPhone { get; set; }

	[Column("phys_email")]
	public string PhysEmail { get; set; } = null!;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public Spec Spec { get; set; } = null!;
	public ICollection<PhysOrg> PhysOrgs { get; set; } = [];
}
