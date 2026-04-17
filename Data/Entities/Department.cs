using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("department")]
public class Department
{
	[Column("department_id")]
	public int DepartmentId { get; set; }

	[Column("department_name")]
	public string DepartmentName { get; set; } = string.Empty;

	[Column("is_deleted")]
	public bool IsDeleted { get; set; }

	public ICollection<UsrDepartment> UsrDepartments { get; set; } = [];
}
