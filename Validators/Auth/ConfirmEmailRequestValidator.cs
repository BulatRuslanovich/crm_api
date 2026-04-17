using CrmWebApi.DTOs.Auth;
using FluentValidation;

namespace CrmWebApi.Validators.Auth;

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
	public ConfirmEmailRequestValidator()
	{
		RuleFor(x => x.Email)
			.NotEmpty()
			.WithMessage("Email обязателен")
			.EmailAddress()
			.WithMessage("Некорректный email");
		RuleFor(x => x.Code).NotEmpty().WithMessage("Код подтверждения обязателен");
	}
}
