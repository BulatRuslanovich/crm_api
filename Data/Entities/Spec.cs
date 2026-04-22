using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("spec")]
public class Spec
{
	[Column("spec_id")]
	public int SpecId { get; init; }

	[MaxLength(100)]
	[Column("spec_name")]
	public string SpecName { get; init; } = null!;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public ICollection<Phys> Physes { get; init; } = [];
}
