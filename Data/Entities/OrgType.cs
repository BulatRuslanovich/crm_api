using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmWebApi.Data.Entities;

[Table("org_type")]
public class OrgType
{
	[Column("org_type_id")]
	public int OrgTypeId { get; init; }

	[MaxLength(100)]
	[Column("org_type_name")]
	public string OrgTypeName { get; init; } = null!;

	public ICollection<Organization> Orgs { get; init; } = [];
}
