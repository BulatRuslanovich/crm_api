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
			.MaximumLength(12)
			.WithMessage("ИНН не должен превышать 12 символов")
			.When(x => x.Inn is not null);
		RuleFor(x => x.Address)
			.MaximumLength(500)
			.WithMessage("Адрес не должен превышать 500 символов")
			.When(x => x.Address is not null);
	}
}
