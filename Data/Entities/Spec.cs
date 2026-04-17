using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("spec")]
public class Spec
{
	[Column("spec_id")]
	public int SpecId { get; set; }

	[Column("spec_name")]
	public string SpecName { get; set; } = null!;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public ICollection<Phys> Physes { get; set; } = [];
}
