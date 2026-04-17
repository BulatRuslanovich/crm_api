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
			.MinimumLength(10)
			.WithMessage("ИНН должен содержать от 10 до 12 символов")
			.MaximumLength(12)
			.WithMessage("ИНН должен содержать от 10 до 12 символов");
		RuleFor(x => x.Address)
			.NotEmpty()
			.WithMessage("Адрес обязателен")
			.MaximumLength(500)
			.WithMessage("Адрес не должен превышать 500 символов");
	}
}
