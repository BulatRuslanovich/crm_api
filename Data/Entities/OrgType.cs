using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("org_type")]
public class OrgType
{
	[Column("org_type_id")]
	public int OrgTypeId { get; set; }

	[Column("org_type_name")]
	public string OrgTypeName { get; set; } = null!;

	public ICollection<Organization> Orgs { get; set; } = [];
}
