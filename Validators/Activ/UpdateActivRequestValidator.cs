using CrmWebApi.DTOs.Activ;
using FluentValidation;

namespace CrmWebApi.Validators.Activ;

public class UpdateActivRequestValidator : AbstractValidator<UpdateActivRequest>
{
	public UpdateActivRequestValidator()
	{
		RuleFor(x => x.StatusId)
			.GreaterThan(0)
			.WithMessage("Статус обязателен")
			.When(x => x.StatusId is not null);
	}
}
