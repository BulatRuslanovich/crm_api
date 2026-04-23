using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("status")]
// ReSharper disable once ClassNeverInstantiated.Global
public class Status
{
	[Key]
	[Column("status_id")]
	// ReSharper disable once UnusedMember.Global
	public int StatusId { get; set; }

	[MaxLength(50)]
	[Column("status_name")]
	public string StatusName { get; set; } = null!;
}
