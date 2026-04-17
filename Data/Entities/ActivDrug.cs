using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("activ_drug")]
public class ActivDrug
{
	[Column("activ_drug_id")]
	public int ActivDrugId { get; set; }

	[Column("activ_id")]
	public int ActivId { get; set; }

	[Column("drug_id")]
	public int DrugId { get; set; }

	public Activ Activ { get; set; } = null!;
	public Drug Drug { get; set; } = null!;
}
