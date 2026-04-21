namespace CrmWebApi.Options;

public sealed class AuthOptions
{
	public const string SectionName = "Auth";

	public bool RequireEmailConfirmation { get; init; } = true;
	public string? OtpHashSecret { get; init; }
}
