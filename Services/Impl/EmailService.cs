using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CrmWebApi.Services.Impl;

public class EmailService(IConfiguration config) : IEmailService
{
	public Task SendEmailConfirmationAsync(string toEmail, string toName, string code)
	{
		var safeName = WebUtility.HtmlEncode(toName);
		return SendAsync(
			toEmail,
			toName,
			subject: "Подтверждение email — CRM",
			html: $"""
			<div style="font-family:sans-serif;max-width:480px;margin:0 auto">
			  <h2 style="color:#2563EB">Подтвердите ваш email</h2>
			  <p>Здравствуйте, {safeName}!</p>
			  <p>Ваш код подтверждения:</p>
			  <div style="font-size:36px;font-weight:700;letter-spacing:8px;color:#2563EB;margin:24px 0">{code}</div>
			  <p style="color:#6B7280;font-size:13px">Код действителен 24 часа. Если вы не регистрировались — проигнорируйте это письмо.</p>
			</div>
			"""
		);
	}

	public Task SendPasswordResetAsync(string toEmail, string toName, string code)
	{
		var safeName = WebUtility.HtmlEncode(toName);
		return SendAsync(
			toEmail,
			toName,
			subject: "Сброс пароля — CRM",
			html: $"""
			<div style="font-family:sans-serif;max-width:480px;margin:0 auto">
			  <h2 style="color:#2563EB">Сброс пароля</h2>
			  <p>Здравствуйте, {safeName}!</p>
			  <p>Ваш код для сброса пароля:</p>
			  <div style="font-size:36px;font-weight:700;letter-spacing:8px;color:#2563EB;margin:24px 0">{code}</div>
			  <p style="color:#6B7280;font-size:13px">Код действителен 1 час. Если вы не запрашивали сброс — проигнорируйте это письмо.</p>
			</div>
			"""
		);
	}

	private const int SmtpTimeoutMs = 15_000;

	private async Task SendAsync(string toEmail, string toName, string subject, string html)
	{
		var host = config["Email:Host"]!;
		var port = int.Parse(config["Email:Port"] ?? "587");
		var username = config["Email:Username"]!;
		var password = config["Email:Password"]!;
		var fromAddress = config["Email:FromAddress"]!;
		var fromName = config["Email:FromName"] ?? "CRM";

		var message = new MimeMessage();
		message.From.Add(new MailboxAddress(fromName, fromAddress));
		message.To.Add(new MailboxAddress(toName, toEmail));
		message.Subject = subject;
		message.Body = new TextPart("html") { Text = html };

		using var client = new SmtpClient();
		for (int i = 0; i < 3; i++)
		{
			try
			{
				using var cts = new CancellationTokenSource(SmtpTimeoutMs);
				await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, cts.Token);
				await client.AuthenticateAsync(username, password, cts.Token);
				await client.SendAsync(message, cts.Token);
				await client.DisconnectAsync(true, cts.Token);
				return;
			}
			catch when (i < 2)
			{
				if (client.IsConnected)
					await client.DisconnectAsync(true);
				await Task.Delay(2000);
			}
		}
	}
}
