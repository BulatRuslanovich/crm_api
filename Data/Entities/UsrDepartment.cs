using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("usr_department")]
public class UsrDepartment
{
	[Column("usr_id")]
	public int UsrId { get; set; }

	[Column("department_id")]
	public int DepartmentId { get; set; }

	[Column("created_at")]
	public DateTimeOffset CreatedAt { get; set; }

	public Usr Usr { get; set; } = null!;
	public Department Department { get; set; } = null!;
}
