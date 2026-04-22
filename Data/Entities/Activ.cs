using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("activ")]
public class Activ
{
	[Column("activ_id")]
	public int ActivId { get; init; }

	[Column("usr_id")]
	public int UsrId { get; init; }

	[Column("org_id")]
	public int? OrgId { get; init; }

	[Column("phys_id")]
	public int? PhysId { get; init; }

	[Column("status_id")]
	public int StatusId { get; set; }

	[Column("activ_start")]
	public DateTimeOffset? ActivStart { get; set; }

	[Column("activ_end")]
	public DateTimeOffset? ActivEnd { get; set; }

	[Column("activ_description")]
	[MaxLength(500)]
	public string ActivDescription { get; set; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public Usr Usr { get; init; } = null!;
	public Organization? Org { get; init; }
	public Phys? Phys { get; init; }
	public Status Status { get; init; } = null!;
	public ICollection<ActivDrug> ActivDrugs { get; init; } = [];
}
