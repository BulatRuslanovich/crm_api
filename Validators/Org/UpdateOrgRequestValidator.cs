using CrmWebApi.DTOs.Org;
using FluentValidation;

namespace CrmWebApi.Validators.Org;

public class UpdateOrgRequestValidator : AbstractValidator<UpdateOrgRequest>
{
	public UpdateOrgRequestValidator()
	{
		RuleFor(x => x.OrgTypeId)
			.GreaterThan(0)
			.WithMessage("Тип организации обязателен")
			.When(x => x.OrgTypeId is not null);
		RuleFor(x => x.OrgName)
			.MaximumLength(200)
			.WithMessage("Название организации не должно превышать 200 символов")
			.When(x => x.OrgName is not null);
		RuleFor(x => x.Inn)
			.Matches(@"^[0-9]{10,12}$")
			.WithMessage("ИНН должен содержать 10–12 цифр")
			.When(x => x.Inn is not null);
		RuleFor(x => x.Address)
			.MaximumLength(500)
			.WithMessage("Адрес не должен превышать 500 символов")
			.When(x => x.Address is not null);
		RuleFor(x => x.Latitude)
			.InclusiveBetween(-90, 90)
			.WithMessage("Широта должна быть в диапазоне [-90, 90]")
			.When(x => x.Latitude is not null);
		RuleFor(x => x.Longitude)
			.InclusiveBetween(-180, 180)
			.WithMessage("Долгота должна быть в диапазоне [-180, 180]")
			.When(x => x.Longitude is not null);
	}
}
