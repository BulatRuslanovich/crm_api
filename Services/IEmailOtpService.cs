using CrmWebApi.Common;
using CrmWebApi.Data.Entities;

namespace CrmWebApi.Services;

public interface IEmailOtpService
{
	public Task<string?> CreateAsync(Usr user, EmailOtpPurpose purpose, TimeSpan ttl);
	public Task<Result> VerifyAsync(int usrId, string code, EmailOtpPurpose purpose);
	public Task DeleteAllAsync(int usrId, EmailOtpPurpose purpose);
}
