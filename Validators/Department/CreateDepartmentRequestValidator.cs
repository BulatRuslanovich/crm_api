using CrmWebApi.DTOs.Department;
using FluentValidation;

namespace CrmWebApi.Validators.Department;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
	public CreateDepartmentRequestValidator()
	{
		RuleFor(x => x.DepartmentName)
			.NotEmpty()
			.WithMessage("Название департамента обязательно")
			.MaximumLength(255)
			.WithMessage("Название не должно превышать 255 символов");
	}
}
