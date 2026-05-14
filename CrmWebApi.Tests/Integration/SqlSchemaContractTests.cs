using CrmWebApi.Common;
using CrmWebApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CrmWebApi.Tests;

[Trait("Category", "Integration")]
public sealed class SqlSchemaContractTests : IAsyncLifetime
{
	private PostgreSqlContainer? _postgres;

	public async Task InitializeAsync()
	{
		_postgres = new PostgreSqlBuilder("postgres:17-alpine")
			.WithDatabase("crm_db")
			.WithUsername("crm_user")
			.WithPassword("crm_password")
			.Build();

		await _postgres.StartAsync();
		await ApplyBootstrapSqlAsync();
	}

	public Task DisposeAsync() => _postgres?.DisposeAsync().AsTask() ?? Task.CompletedTask;

	[Fact(DisplayName = "SQL bootstrap schema matches EF entity mappings")]
	public async Task SqlBootstrapSchema_MatchesEfEntityMappings()
	{
		await using var db = CreateDbContext();
		var storeObjectColumns = await LoadDatabaseColumnsAsync();
		var missing = new List<string>();

		foreach (var entityType in db.Model.GetEntityTypes())
		{
			var tableName = entityType.GetTableName();
			if (tableName is null)
				continue;

			var schema = entityType.GetSchema() ?? "public";
			if (!storeObjectColumns.TryGetValue((schema, tableName), out var columns))
			{
				missing.Add($"{entityType.ClrType.Name} -> table {schema}.{tableName}");
				continue;
			}

			var storeObject = StoreObjectIdentifier.Table(tableName, schema);
			foreach (var property in entityType.GetProperties())
			{
				var columnName = property.GetColumnName(storeObject);
				if (columnName is not null && !columns.Contains(columnName))
					missing.Add($"{entityType.ClrType.Name}.{property.Name} -> column {schema}.{tableName}.{columnName}");
			}
		}

		Assert.Empty(missing);
	}

	[Fact(DisplayName = "SQL bootstrap supports representative EF queries")]
	public async Task SqlBootstrapSchema_SupportsRepresentativeEfQueries()
	{
		await using var db = CreateDbContext();

		Assert.True(await db.Policies.AnyAsync(p => p.PolicyName == RoleNames.Admin));
		Assert.True(await db.Usrs.AnyAsync(u => u.UsrLogin == "admin"));
		Assert.True(await db.OrgTypes.AnyAsync());
		Assert.True(await db.AssistantConversations.CountAsync() == 0);
	}

	private AppDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseNpgsql(GetConnectionString())
			.Options;

		return new AppDbContext(options);
	}

	private async Task ApplyBootstrapSqlAsync()
	{
		var sqlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "sql-scripts", "01-init-tables.sql"));
		var sql = await File.ReadAllTextAsync(sqlPath);

		await using var connection = new NpgsqlConnection(GetConnectionString());
		await connection.OpenAsync();
		await using var command = new NpgsqlCommand(sql, connection);
		await command.ExecuteNonQueryAsync();
	}

	private async Task<Dictionary<(string Schema, string Table), HashSet<string>>> LoadDatabaseColumnsAsync()
	{
		const string sql = """
			SELECT table_schema, table_name, column_name
			FROM information_schema.columns
			WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
			""";

		var result = new Dictionary<(string Schema, string Table), HashSet<string>>();
		await using var connection = new NpgsqlConnection(GetConnectionString());
		await connection.OpenAsync();
		await using var command = new NpgsqlCommand(sql, connection);
		await using var reader = await command.ExecuteReaderAsync();

		while (await reader.ReadAsync())
		{
			var key = (reader.GetString(0), reader.GetString(1));
			if (!result.TryGetValue(key, out var columns))
			{
				columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				result[key] = columns;
			}

			columns.Add(reader.GetString(2));
		}

		return result;
	}

	private string GetConnectionString() =>
		_postgres?.GetConnectionString() ?? throw new InvalidOperationException("PostgreSQL test container was not initialized.");
}
