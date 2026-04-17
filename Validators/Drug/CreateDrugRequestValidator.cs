using CrmWebApi.DTOs.Drug;
using FluentValidation;

namespace CrmWebApi.Validators.Drug;

public class CreateDrugRequestValidator : AbstractValidator<CreateDrugRequest>
{
	public CreateDrugRequestValidator()
	{
		RuleFor(x => x.DrugName)
			.NotEmpty()
			.WithMessage("Название препарата обязательно")
			.MaximumLength(200)
			.WithMessage("Название препарата не должно превышать 200 символов");
		RuleFor(x => x.Brand)
			.MaximumLength(200)
			.WithMessage("Бренд не должен превышать 200 символов")
			.When(x => x.Brand is not null);
		RuleFor(x => x.Form)
			.MaximumLength(100)
			.WithMessage("Форма выпуска не должна превышать 100 символов")
			.When(x => x.Form is not null);
	}
}
