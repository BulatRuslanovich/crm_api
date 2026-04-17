using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("usr")]
public class Usr
{
	[Column("usr_id")]
	public int UsrId { get; set; }

	[Column("usr_firstname")]
	public string UsrFirstname { get; set; } = string.Empty;

	[Column("usr_lastname")]
	public string UsrLastname { get; set; } = string.Empty;

	[Column("usr_email")]
	public string UsrEmail { get; set; } = string.Empty;

	[Column("usr_login")]
	public string UsrLogin { get; set; } = string.Empty;

	[Column("usr_password_hash")]
	public string UsrPasswordHash { get; set; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	[Column("is_email_confirmed")]
	public bool IsEmailConfirmed { get; set; }

	public ICollection<UsrPolicy> UsrPolicies { get; set; } = [];
	public ICollection<UsrDepartment> UsrDepartments { get; set; } = [];
	public ICollection<Activ> Activs { get; set; } = [];
}
