using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("status")]
public abstract class Status(string statusName)
{
	[MaxLength(50)]
	[Column("status_name")]
	public string StatusName { get; } = statusName;
}
