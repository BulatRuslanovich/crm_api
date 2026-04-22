using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("department")]
public class Department
{
	[Column("department_id")]
	public int DepartmentId { get; init; }


	[Column("department_name")]
	[MaxLength(255)]
	public string DepartmentName { get; init; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public ICollection<UsrDepartment> UsrDepartments { get; init; } = [];
}
