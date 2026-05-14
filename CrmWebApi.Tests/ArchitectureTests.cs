using CrmWebApi.Common;
using CrmWebApi.Repositories;
using CrmWebApi.Services;

namespace CrmWebApi.Tests;

public sealed class ArchitectureTests
{
	[Fact]
	public void ScopedServiceContracts_DoNotAcceptScopeFromControllers()
	{
		var scopedContracts = new[] { typeof(IActivService), typeof(IUserService) };

		var offenders = scopedContracts
			.SelectMany(type => type.GetMethods().Select(method => new { type, method }))
			.SelectMany(x => x.method.GetParameters()
				.Where(parameter => parameter.ParameterType == typeof(Scope))
				.Select(parameter => $"{x.type.Name}.{x.method.Name}({parameter.Name})"))
			.ToArray();

		Assert.Empty(offenders);
	}

	[Fact]
	public void RepositoryContracts_DoNotExposeIQueryable()
	{
		var repositoryContracts = new[]
		{
			typeof(IActivRepository),
			typeof(IUserRepository),
			typeof(IOrgRepository),
			typeof(IPhysRepository),
			typeof(IDepartmentRepository),
		};

		var offenders = repositoryContracts
			.SelectMany(type => type.GetMethods().Select(method => new { type, method }))
			.Where(x =>
				x.method.ReturnType.IsGenericType
				&& x.method.ReturnType.GetGenericTypeDefinition() == typeof(IQueryable<>)
			)
			.Select(x => $"{x.type.Name}.{x.method.Name}")
			.ToArray();

		Assert.Empty(offenders);
	}
}
