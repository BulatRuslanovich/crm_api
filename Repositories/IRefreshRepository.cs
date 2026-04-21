using CrmWebApi.Data.Entities;

namespace CrmWebApi.Repositories;

public interface IRefreshRepository
{
	public Task<Refresh> AddAsync(Refresh entity);
	public Task DeleteAsync(Refresh entity);
	public Task<Refresh?> GetByTokenHashAsync(string tokenHash);
	public Task<Refresh?> ConsumeByTokenHashAsync(string tokenHash);
	public Task RevokeAllForUserAsync(int usrId);
}
