using CrmWebApi.Data.Entities;

namespace CrmWebApi.Repositories;

public interface IEmailTokenRepository
{
	public Task<EmailToken?> CreateIfNoActiveAsync(EmailToken entity);
	public Task UpdateAsync(EmailToken entity);
	public Task<EmailToken?> GetActiveByUserAndTypeAsync(int usrId, int tokenType);
	public Task DeleteAllForUserAsync(int usrId, int tokenType);
}
