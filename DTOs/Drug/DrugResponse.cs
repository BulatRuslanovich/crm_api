namespace CrmWebApi.DTOs.Drug;

public record DrugResponse(int DrugId, string DrugName, string Brand, string Form)
{
	public static DrugResponse From(Data.Entities.Drug d) =>
		new(d.DrugId, d.DrugName, d.DrugBrand, d.DrugForm);
}
