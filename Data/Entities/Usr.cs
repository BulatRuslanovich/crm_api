using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("usr")]
public class Usr
{
	[Column("usr_id")]
	public int UsrId { get; init; }

	[MaxLength(100)]
	[Column("usr_firstname")]
	public string UsrFirstname { get; set; } = string.Empty;

	[MaxLength(100)]
	[Column("usr_lastname")]
	public string UsrLastname { get; set; } = string.Empty;

	[MaxLength(150)]
	[Column("usr_email")]
	public string UsrEmail { get; init; } = string.Empty;

	[MaxLength(100)]
	[Column("usr_login")]
	public string UsrLogin { get; init; } = string.Empty;

	[MaxLength(255)]
	[Column("usr_password_hash")]
	public string UsrPasswordHash { get; set; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	[Column("is_email_confirmed")]
	public bool IsEmailConfirmed { get; set; }

	public ICollection<UsrPolicy> UsrPolicies { get; init; } = [];
	public ICollection<UsrDepartment> UsrDepartments { get; init; } = [];
	public ICollection<Activ> Activs { get; init; } = [];
}
