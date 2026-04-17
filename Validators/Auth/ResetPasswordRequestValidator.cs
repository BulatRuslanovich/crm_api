using CrmWebApi.DTOs.Auth;
using FluentValidation;

namespace CrmWebApi.Validators.Auth;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
	public ResetPasswordRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Email обязателен")
			.EmailAddress()
			.WithMessage("Некорректный email");
		RuleFor(x => x.Code).NotEmpty().WithMessage("Код подтверждения обязателен");
		RuleFor(x => x.NewPassword)
			.NotEmpty()
			.WithMessage("Новый пароль обязателен")
			.MinimumLength(6)
			.WithMessage("Пароль должен содержать не менее 6 символов")
			.MaximumLength(100)
			.WithMessage("Пароль не должен превышать 100 символов");
	}
}
