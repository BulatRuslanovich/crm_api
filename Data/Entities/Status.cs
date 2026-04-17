using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("status")]
public class Status
{
	[Column("status_id")]
	public int StatusId { get; set; }

	[Column("status_name")]
	public string StatusName { get; set; } = string.Empty;

	public ICollection<Activ> Activs { get; set; } = [];
}
