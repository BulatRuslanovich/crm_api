using CrmWebApi.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Health;

public class SmtpHealthCheck(IOptions<EmailOptions> options) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default
	)
	{
		var emailOptions = options.Value;

		try
		{
			using var client = new SmtpClient();
			await client.ConnectAsync(
				emailOptions.Host,
				emailOptions.Port,
				SecureSocketOptions.StartTls,
				cancellationToken
			);
			await client.AuthenticateAsync(
				emailOptions.Username,
				emailOptions.Password,
				cancellationToken
			);
			await client.DisconnectAsync(true, cancellationToken);
			return HealthCheckResult.Healthy("SMTP connection OK");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("SMTP connection failed", ex);
		}
	}
}
