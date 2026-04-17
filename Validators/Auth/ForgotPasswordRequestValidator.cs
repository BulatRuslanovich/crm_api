using CrmWebApi.DTOs.Auth;
using FluentValidation;

namespace CrmWebApi.Validators.Auth;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
	public ForgotPasswordRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Email обязателен")
			.EmailAddress()
			.WithMessage("Некорректный email");
	}
}
