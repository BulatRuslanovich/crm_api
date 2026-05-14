using System.Net;
using CrmWebApi.Common;

namespace CrmWebApi.Tests;

[Trait("Controller", "Departments")]
public sealed class DepartmentsControllerContractTests(ApiTestFactory factory)
	: ContractTestBase(factory), IClassFixture<ApiTestFactory>
{
	[Fact(DisplayName = "Departments GET list returns paged response")]
	public async Task GetAll_WithAdminToken_ReturnsPagedResponse()
	{
		// Arrange: departments are admin-only.
		using var request = AuthorizedGet("/api/departments", RoleNames.Admin);

		// Act: request the departments list.
		var response = await Client.SendAsync(request);

		// Assert: admins receive the standard PagedResponse shape.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var json = await ReadJsonAsync(response);
		Assert.Equal(1, json.GetProperty("totalCount").GetInt32());
		Assert.Equal("Продажи", json.GetProperty("items")[0].GetProperty("departmentName").GetString());
	}

	[Fact(DisplayName = "Departments GET list returns 403 ProblemDetails for non-admin role")]
	public async Task GetAll_WithRepresentativeToken_ReturnsForbiddenProblemDetails()
	{
		// Arrange: representatives are authenticated but do not satisfy the Admin role policy.
		using var request = AuthorizedGet("/api/departments", RoleNames.Representative);

		// Act: request an admin-only endpoint.
		var response = await Client.SendAsync(request);

		// Assert: authorization failures use the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}

	[Fact(DisplayName = "Departments GET by id returns item response")]
	public async Task GetById_WithAdminToken_ReturnsDepartment()
	{
		// Arrange: request an existing fake department id.
		using var request = AuthorizedGet("/api/departments/1", RoleNames.Admin);

		// Act: request department by id.
		var response = await Client.SendAsync(request);

		// Assert: the response contains the requested department.
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}

	[Fact(DisplayName = "Departments POST creates item and returns 201")]
	public async Task Create_WithAdminToken_ReturnsCreated()
	{
		// Arrange: valid department create payload.
		var body = new { departmentName = "Маркетинг" };
		using var request = AuthorizedJson(HttpMethod.Post, "/api/departments", RoleNames.Admin, body);

		// Act: create the department.
		var response = await Client.SendAsync(request);

		// Assert: successful creation returns Created.
		Assert.Equal(HttpStatusCode.Created, response.StatusCode);
	}

	[Fact(DisplayName = "Departments POST user membership returns 204")]
	public async Task AddUser_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create a department membership request.
		using var request = AuthorizedRequest(
			HttpMethod.Post,
			"/api/departments/1/users/1",
			RoleNames.Admin
		);

		// Act: add the user to the department.
		var response = await Client.SendAsync(request);

		// Assert: successful membership add returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Departments DELETE user membership returns 204")]
	public async Task RemoveUser_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create a department membership removal request.
		using var request = AuthorizedRequest(
			HttpMethod.Delete,
			"/api/departments/1/users/1",
			RoleNames.Admin
		);

		// Act: remove the user from the department.
		var response = await Client.SendAsync(request);

		// Assert: successful membership removal returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Departments DELETE removes item and returns 204")]
	public async Task Delete_WithAdminToken_ReturnsNoContent()
	{
		// Arrange: create an admin DELETE request.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/departments/1", RoleNames.Admin);

		// Act: delete the department.
		var response = await Client.SendAsync(request);

		// Assert: successful deletion returns 204.
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
	}

	[Fact(DisplayName = "Departments GET by id returns 404 for missing item")]
	public async Task GetById_ReturnsNotFound_WhenDepartmentDoesNotExist()
	{
		// Arrange: request a non-existent department id.
		using var request = AuthorizedGet("/api/departments/999", RoleNames.Admin);

		// Act: call the item endpoint with an unknown id.
		var response = await Client.SendAsync(request);

		// Assert: missing department maps to 404 ProblemDetails.
		await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound, "Отдел 999 не найден");
	}

	[Fact(DisplayName = "Departments POST returns 403 for non-admin role")]
	public async Task Create_WithRepresentativeToken_ReturnsForbidden()
	{
		// Arrange: representatives are not allowed to create departments.
		using var request = AuthorizedRequest(HttpMethod.Post, "/api/departments", RoleNames.Representative);

		// Act: attempt to create a department without the required role.
		var response = await Client.SendAsync(request);

		// Assert: role failure uses the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}

	[Fact(DisplayName = "Departments DELETE returns 403 for non-admin role")]
	public async Task Delete_WithRepresentativeToken_ReturnsForbidden()
	{
		// Arrange: representatives are not allowed to delete departments.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/departments/1", RoleNames.Representative);

		// Act: attempt to delete a department without the required role.
		var response = await Client.SendAsync(request);

		// Assert: role failure uses the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}

	[Fact(DisplayName = "Departments POST user membership returns 403 for non-admin role")]
	public async Task AddUser_WithRepresentativeToken_ReturnsForbidden()
	{
		// Arrange: only admins can add users to departments.
		using var request = AuthorizedRequest(HttpMethod.Post, "/api/departments/1/users/1", RoleNames.Representative);

		// Act: attempt to add a user without the required role.
		var response = await Client.SendAsync(request);

		// Assert: role failure uses the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}

	[Fact(DisplayName = "Departments DELETE user membership returns 403 for non-admin role")]
	public async Task RemoveUser_WithRepresentativeToken_ReturnsForbidden()
	{
		// Arrange: only admins can remove users from departments.
		using var request = AuthorizedRequest(HttpMethod.Delete, "/api/departments/1/users/1", RoleNames.Representative);

		// Act: attempt to remove a user without the required role.
		var response = await Client.SendAsync(request);

		// Assert: role failure uses the shared 403 ProblemDetails contract.
		await AssertProblemDetailsAsync(response, HttpStatusCode.Forbidden, "Доступ запрещён");
	}
}
