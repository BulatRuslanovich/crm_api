using CrmWebApi.DTOs.Auth;
using FluentValidation;

namespace CrmWebApi.Validators.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
	public RegisterRequestValidator()
	{
		RuleFor(x => x.Login)
			.NotEmpty()
			.WithMessage("Логин обязателен")
			.MaximumLength(100)
			.WithMessage("Логин не должен превышать 100 символов");
		RuleFor(x => x.Password)
			.NotEmpty()
			.WithMessage("Пароль обязателен")
			.MinimumLength(8)
			.WithMessage("Пароль должен содержать не менее 8 символов")
			.MaximumLength(100)
			.WithMessage("Пароль не должен превышать 100 символов")
			.Must(p => p.Any(char.IsDigit))
			.WithMessage("Пароль должен содержать хотя бы одну цифру");
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Email обязателен")
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
	}
}
