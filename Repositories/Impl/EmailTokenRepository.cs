using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class EmailTokenRepository(AppDbContext db) : IEmailTokenRepository
{
	public async Task<EmailToken?> CreateIfNoActiveAsync(EmailToken entity)
	{
		await using var tx = await db.Database.BeginTransactionAsync();

		await db.Database.ExecuteSqlInterpolatedAsync(
			$"SELECT pg_advisory_xact_lock({entity.UsrId}, {entity.TokenType})"
		);

		var hasActive = await db.EmailTokens.AnyAsync(t =>
			t.UsrId == entity.UsrId
			&& t.TokenType == entity.TokenType
			&& t.ExpiresAt > DateTime.UtcNow
		);
		if (hasActive)
			return null;

		await db
			.EmailTokens.Where(t => t.UsrId == entity.UsrId && t.TokenType == entity.TokenType)
			.ExecuteDeleteAsync();

		db.EmailTokens.Add(entity);
		await db.SaveChangesAsync();
		await tx.CommitAsync();

		return entity;
	}

	public async Task UpdateAsync(EmailToken entity)
	{
		db.EmailTokens.Update(entity);
		await db.SaveChangesAsync();
	}

	public Task<EmailToken?> GetActiveByUserAndTypeAsync(int usrId, int tokenType) =>
		db.EmailTokens.FirstOrDefaultAsync(t =>
			t.UsrId == usrId && t.TokenType == tokenType && t.ExpiresAt > DateTime.UtcNow
		);

	public Task DeleteAllForUserAsync(int usrId, int tokenType) =>
		db
			.EmailTokens.Where(t => t.UsrId == usrId && t.TokenType == tokenType)
			.ExecuteDeleteAsync();
}
