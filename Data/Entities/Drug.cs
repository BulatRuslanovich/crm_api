using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("drug")]
public class Drug
{
	[Column("drug_id")]
	public int DrugId { get; set; }

	[Column("drug_name")]
	public string DrugName { get; set; } = null!;

	[Column("drug_brand")]
	public string DrugBrand { get; set; } = null!;

	[Column("drug_form")]
	public string DrugForm { get; set; } = null!;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public ICollection<ActivDrug> ActivDrugs { get; set; } = [];
}
