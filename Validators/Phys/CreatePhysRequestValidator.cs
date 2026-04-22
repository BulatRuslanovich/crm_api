using CrmWebApi.DTOs.Phys;
using FluentValidation;

namespace CrmWebApi.Validators.Phys;

public class CreatePhysRequestValidator : AbstractValidator<CreatePhysRequest>
{
	public CreatePhysRequestValidator()
	{
		RuleFor(x => x.LastName)
			.NotEmpty()
			.WithMessage("Фамилия обязательна")
			.MaximumLength(100)
			.WithMessage("Фамилия не должна превышать 100 символов");
		RuleFor(x => x.FirstName)
			.MaximumLength(100)
			.WithMessage("Имя не должно превышать 100 символов");
		RuleFor(x => x.MiddleName)
			.MaximumLength(100)
			.WithMessage("Отчество не должно превышать 100 символов");
		RuleFor(x => x.Email)
			.EmailAddress()
			.WithMessage("Некорректный email")
			.MaximumLength(200)
			.WithMessage("Email не должен превышать 200 символов");
		RuleFor(x => x.Phone)
			.MaximumLength(20)
			.WithMessage("Телефон не должен превышать 20 символов");
	}
}
