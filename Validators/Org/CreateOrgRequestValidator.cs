using CrmWebApi.DTOs.Org;
using FluentValidation;

namespace CrmWebApi.Validators.Org;

public class CreateOrgRequestValidator : AbstractValidator<CreateOrgRequest>
{
	public CreateOrgRequestValidator()
	{
		RuleFor(x => x.OrgTypeId).GreaterThan(0).WithMessage("Тип организации обязателен");
		RuleFor(x => x.OrgName)
			.NotEmpty()
			.WithMessage("Название организации обязательно")
			.MaximumLength(200)
			.WithMessage("Название организации не должно превышать 200 символов");
		RuleFor(x => x.Inn)
			.NotEmpty()
			.WithMessage("ИНН обязателен")
			.Matches(@"^[0-9]{10,12}$")
			.WithMessage("ИНН должен содержать 10–12 цифр");
		RuleFor(x => x.Address)
			.NotEmpty()
			.WithMessage("Адрес обязателен")
			.MaximumLength(500)
			.WithMessage("Адрес не должен превышать 500 символов");
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
