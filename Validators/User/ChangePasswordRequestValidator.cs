using CrmWebApi.DTOs.User;
using FluentValidation;

namespace CrmWebApi.Validators.User;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
	public ChangePasswordRequestValidator()
	{
		RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Текущий пароль обязателен");
		RuleFor(x => x.NewPassword)
			.NotEmpty()
			.WithMessage("Новый пароль обязателен")
			.MinimumLength(6)
			.WithMessage("Пароль должен содержать не менее 6 символов")
			.MaximumLength(100)
			.WithMessage("Пароль не должен превышать 100 символов");
	}
}
