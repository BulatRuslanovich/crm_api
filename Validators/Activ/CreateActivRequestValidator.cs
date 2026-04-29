using CrmWebApi.DTOs.Activ;
using FluentValidation;

namespace CrmWebApi.Validators.Activ;

public class CreateActivRequestValidator : AbstractValidator<CreateActivRequest>
{
	public CreateActivRequestValidator()
	{
		RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("Статус обязателен");

		RuleFor(x => x.Latitude)
			.InclusiveBetween(-90, 90)
			.WithMessage("Широта должна быть в диапазоне [-90, 90]")
			.When(x => x.Latitude is not null);

		RuleFor(x => x.Longitude)
			.InclusiveBetween(-180, 180)
			.WithMessage("Долгота должна быть в диапазоне [-180, 180]")
			.When(x => x.Longitude is not null);

		RuleFor(x => x)
			.Must(r => r.End is null || r.End >= r.Start)
			.WithMessage("Дата окончания не может быть раньше даты начала");
	}
}
