using CrmWebApi.DTOs.User;
using FluentValidation;

namespace CrmWebApi.Validators.User;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
	public UpdateUserRequestValidator()
	{
		RuleFor(x => x.FirstName)
			.MaximumLength(100)
			.WithMessage("Имя не должно превышать 100 символов")
			.When(x => x.FirstName is not null)
			.WithMessage("Имя должно быть заполнено");
		RuleFor(x => x.LastName)
			.MaximumLength(100)
			.WithMessage("Фамилия не должна превышать 100 символов")
			.When(x => x.LastName is not null)
			.WithMessage("Фамилия должна быть заполнена");
	}
}
