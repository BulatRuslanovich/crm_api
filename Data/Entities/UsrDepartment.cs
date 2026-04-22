using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("usr_department")]
public class UsrDepartment
{
	[Column("usr_id")]
	public int UsrId { get; init; }

	[Column("department_id")]
	public int DepartmentId { get; init; }

	[Column("created_at")]
	public DateTimeOffset CreatedAt { get; init; }

	public Usr Usr { get; init; } = null!;
	public Department Department { get; init; } = null!;
}
