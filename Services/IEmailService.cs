namespace CrmWebApi.Services;

public interface IEmailService
{
	public Task SendEmailConfirmationAsync(string toEmail, string toName, string code);
	public Task SendPasswordResetAsync(string toEmail, string toName, string code);
}
