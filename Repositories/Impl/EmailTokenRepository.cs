using CrmWebApi.Data;
using CrmWebApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CrmWebApi.Repositories.Impl;

public class EmailTokenRepository(AppDbContext db) : IEmailTokenRepository
{
	public async Task<EmailToken> AddAsync(EmailToken entity)
	{
		db.EmailTokens.Add(entity);
		await db.SaveChangesAsync();
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
