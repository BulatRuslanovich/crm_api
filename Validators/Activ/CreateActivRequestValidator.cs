using CrmWebApi.DTOs.Activ;
using FluentValidation;

namespace CrmWebApi.Validators.Activ;

public class CreateActivRequestValidator : AbstractValidator<CreateActivRequest>
{
	public CreateActivRequestValidator()
	{
		RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("Статус обязателен");
		RuleFor(x => x.DrugIds).NotNull().WithMessage("Список препаратов обязателен");

		RuleFor(x => x)
			.Must(r => (r.OrgId.HasValue && r.OrgId > 0) ^ (r.PhysId.HasValue && r.PhysId > 0))
			.WithMessage("Нужно указать либо организацию, либо физика — но не оба");
	}
}
