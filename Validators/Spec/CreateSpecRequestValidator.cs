using CrmWebApi.DTOs.Spec;
using FluentValidation;

namespace CrmWebApi.Validators.Spec;

public class CreateSpecRequestValidator : AbstractValidator<CreateSpecRequest>
{
	public CreateSpecRequestValidator()
	{
		RuleFor(x => x.SpecName)
			.NotEmpty()
			.WithMessage("Название специализации обязательно")
			.MaximumLength(200)
			.WithMessage("Название специализации не должно превышать 200 символов");
	}
}
