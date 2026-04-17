using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("activ")]
public class Activ
{
	[Column("activ_id")]
	public int ActivId { get; set; }

	[Column("usr_id")]
	public int UsrId { get; set; }

	[Column("org_id")]
	public int? OrgId { get; set; }

	[Column("phys_id")]
	public int? PhysId { get; set; }

	[Column("status_id")]
	public int StatusId { get; set; }

	[Column("activ_start")]
	public DateTimeOffset? ActivStart { get; set; }

	[Column("activ_end")]
	public DateTimeOffset? ActivEnd { get; set; }

	[Column("activ_description")]
	public string ActivDescription { get; set; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public Usr Usr { get; set; } = null!;
	public Organization? Org { get; set; }
	public Phys? Phys { get; set; }
	public Status Status { get; set; } = null!;
	public ICollection<ActivDrug> ActivDrugs { get; set; } = [];
}
