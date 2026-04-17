using CrmWebApi.DTOs.Auth;
using FluentValidation;

namespace CrmWebApi.Validators.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
	public LoginRequestValidator()
	{
		RuleFor(x => x.Login).NotEmpty().WithMessage("Логин обязателен");
		RuleFor(x => x.Password).NotEmpty().WithMessage("Пароль обязателен");
	}
}
