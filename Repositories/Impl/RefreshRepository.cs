using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class RefreshRepository(AppDbContext db) : IRefreshRepository
{
	public async Task<Refresh> AddAsync(Refresh entity)
	{
		db.Refreshes.Add(entity);
		await db.SaveChangesAsync();
		return entity;
	}

	public async Task DeleteAsync(Refresh entity)
	{
		db.Refreshes.Remove(entity);
		await db.SaveChangesAsync();
	}

	public Task<Refresh?> GetByTokenHashAsync(string tokenHash) =>
		db.Refreshes.FirstOrDefaultAsync(r => r.RefreshTokenHash == tokenHash);

	public async Task<Refresh?> ConsumeByTokenHashAsync(string tokenHash)
	{
		await db.Database.OpenConnectionAsync();
		try
		{
			await using var command = db.Database.GetDbConnection().CreateCommand();
			command.CommandText = """
				DELETE FROM refresh
				WHERE refresh_token_hash = @tokenHash
				RETURNING refresh_id, usr_id, refresh_token_hash, refresh_expires_at
				""";

			var parameter = command.CreateParameter();
			parameter.ParameterName = "tokenHash";
			parameter.Value = tokenHash;
			command.Parameters.Add(parameter);

			await using var reader = await command.ExecuteReaderAsync();
			if (!await reader.ReadAsync())
				return null;

			return new Refresh
			{
				RefreshId = reader.GetInt64(0),
				UsrId = reader.GetInt32(1),
				RefreshTokenHash = reader.GetString(2),
				RefreshExpiresAt = reader.GetFieldValue<DateTime>(3),
			};
		}
		finally
		{
			await db.Database.CloseConnectionAsync();
		}
	}

	public Task RevokeAllForUserAsync(int usrId) =>
		db.Refreshes.Where(r => r.UsrId == usrId).ExecuteDeleteAsync();
}
