using CrmWebApi.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CrmWebApi.Filters;

public class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
	public async Task OnActionExecutionAsync(
		ActionExecutingContext context,
		ActionExecutionDelegate next
	)
	{
		foreach (var (_, value) in context.ActionArguments)
		{
			if (value is null)
				continue;
			var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
			if (services.GetService(validatorType) is not IValidator validator)
				continue;

			var result = await validator.ValidateAsync(new ValidationContext<object>(value));
			if (result.IsValid)
				continue;

			var errors = result
				.Errors.GroupBy(e => e.PropertyName)
				.ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

			context.Result = ApiProblemDetails.ToActionResult(
				ApiProblemDetails.FromValidationErrors(errors, context.HttpContext)
			);
			return;
		}

		await next();
	}
}
