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

	public Task<Refresh?> ConsumeByTokenHashAsync(string tokenHash) =>
		db.Refreshes
			.FromSqlRaw("""
			                DELETE FROM refresh
			                WHERE refresh_token_hash = {0}
			                RETURNING refresh_id, usr_id, refresh_token_hash, refresh_expires_at
			            """, tokenHash)
			.AsNoTracking()
			.FirstOrDefaultAsync();

	public Task RevokeAllForUserAsync(int usrId) =>
		db.Refreshes.Where(r => r.UsrId == usrId).ExecuteDeleteAsync();
}
