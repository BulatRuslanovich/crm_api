using CrmWebApi.Common;
using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using CrmWebApi.DTOs.Drug;
using CrmWebApi.Services.Impl;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Tests;

public sealed class DrugServiceTests
{
	private static AppDbContext CreateContext() =>
		new(new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"drug-tests-{Guid.NewGuid()}")
			.Options);

	private static async Task<AppDbContext> SeedAsync()
	{
		var db = CreateContext();
		db.Drugs.AddRange(
			new Drug { DrugName = "Амоксициллин", DrugBrand = "Флемоксин", DrugForm = "таблетки" },
			new Drug { DrugName = "Ибупрофен", DrugBrand = "Нурофен", DrugForm = "капсулы" },
			new Drug { DrugName = "Парацетамол", DrugBrand = "Панадол", DrugForm = "таблетки" }
		);
		await db.SaveChangesAsync();
		return db;
	}

	[Fact]
	public async Task GetAllAsync_ReturnsPaginatedResults()
	{
		await using var db = await SeedAsync();
		var service = new DrugService(db);

		var result = await service.GetAllAsync(page: 1, pageSize: 2);

		Assert.True(result.IsSuccess);
		Assert.Equal(2, result.Value!.Items.Count());
		Assert.Equal(3, result.Value.TotalCount);
		Assert.Equal(1, result.Value.Page);
	}

	[Fact]
	public async Task GetAllAsync_SecondPage_ReturnsRemainingItems()
	{
		await using var db = await SeedAsync();
		var service = new DrugService(db);

		var result = await service.GetAllAsync(page: 2, pageSize: 2);

		Assert.True(result.IsSuccess);
		Assert.Single(result.Value!.Items);
	}

	[Fact]
	public async Task GetAllAsync_WithIncludeTotalFalse_ReturnsZeroTotal()
	{
		await using var db = await SeedAsync();
		var service = new DrugService(db);

		var result = await service.GetAllAsync(page: 1, pageSize: 10, includeTotal: false);

		Assert.True(result.IsSuccess);
		Assert.Equal(0, result.Value!.TotalCount);
		Assert.Equal(3, result.Value.Items.Count());
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsDrug_WhenExists()
	{
		await using var db = await SeedAsync();
		var drug = await db.Drugs.FirstAsync();
		var service = new DrugService(db);

		var result = await service.GetByIdAsync(drug.DrugId);

		Assert.True(result.IsSuccess);
		Assert.Equal(drug.DrugId, result.Value!.DrugId);
		Assert.Equal("Амоксициллин", result.Value.DrugName);
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsNotFound_WhenMissing()
	{
		await using var db = CreateContext();
		var service = new DrugService(db);

		var result = await service.GetByIdAsync(999);

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.NotFound, result.Error!.Type);
	}

	[Fact]
	public async Task CreateAsync_PersistsAndReturnsDrug()
	{
		await using var db = CreateContext();
		var service = new DrugService(db);

		var result = await service.CreateAsync(new CreateDrugRequest("Цетиризин", "Зиртек", "таблетки"));

		Assert.True(result.IsSuccess);
		Assert.Equal("Цетиризин", result.Value!.DrugName);
		Assert.Equal("Зиртек", result.Value.Brand);
		Assert.Equal(1, await db.Drugs.CountAsync());
	}

	[Fact]
	public async Task UpdateAsync_UpdatesFields_WhenExists()
	{
		await using var db = await SeedAsync();
		var drug = await db.Drugs.FirstAsync();
		var service = new DrugService(db);

		var result = await service.UpdateAsync(drug.DrugId, new UpdateDrugRequest("Новое название", null, null));

		Assert.True(result.IsSuccess);
		Assert.Equal("Новое название", result.Value!.DrugName);
		Assert.Equal(drug.DrugBrand, result.Value.Brand);
	}

	[Fact]
	public async Task UpdateAsync_ReturnsNotFound_WhenMissing()
	{
		await using var db = CreateContext();
		var service = new DrugService(db);

		var result = await service.UpdateAsync(999, new UpdateDrugRequest("Что-то", null, null));

		Assert.False(result.IsSuccess);
		Assert.Equal(ErrorType.NotFound, result.Error!.Type);
	}
}
