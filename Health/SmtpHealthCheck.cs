using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CrmWebApi.Health;

public class SmtpHealthCheck(IConfiguration config) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default
	)
	{
		var host = config["Email:Host"]!;
		var port = int.Parse(config["Email:Port"] ?? "587");
		var username = config["Email:Username"]!;
		var password = config["Email:Password"]!;

		try
		{
			using var client = new SmtpClient();
			await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
			await client.AuthenticateAsync(username, password, cancellationToken);
			await client.DisconnectAsync(true, cancellationToken);
			return HealthCheckResult.Healthy("SMTP connection OK");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("SMTP connection failed", ex);
		}
	}
}
