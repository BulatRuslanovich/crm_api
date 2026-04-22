using CrmWebApi.DTOs.User;
using FluentValidation;

namespace CrmWebApi.Validators.User;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
	public CreateUserRequestValidator()
	{
		RuleFor(x => x.Login)
			.NotEmpty()
			.WithMessage("Логин обязателен")
			.MaximumLength(100)
			.WithMessage("Логин не должен превышать 100 символов");
		RuleFor(x => x.Password)
			.NotEmpty()
			.WithMessage("Пароль обязателен")
			.MinimumLength(6)
			.WithMessage("Пароль должен содержать не менее 6 символов")
			.MaximumLength(100)
			.WithMessage("Пароль не должен превышать 100 символов");
		RuleFor(x => x.Email)
			.EmailAddress()
			.WithMessage("Некорректный email")
			.MaximumLength(200)
			.WithMessage("Email не должен превышать 200 символов");
		RuleFor(x => x.FirstName)
			.MaximumLength(100)
			.WithMessage("Имя не должно превышать 100 символов");
		RuleFor(x => x.LastName)
			.MaximumLength(100)
			.WithMessage("Фамилия не должна превышать 100 символов");
		RuleFor(x => x.PolicyIds).NotNull().WithMessage("Список ролей обязателен");
	}
}
