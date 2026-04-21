namespace CrmWebApi.Options;

public sealed class JwtOptions
{
	public const string SectionName = "Jwt";
	private const string PlaceholderSecret = "REPLACE_WITH_MIN_32_CHAR_SECRET_KEY_HERE";

	public string Secret { get; init; } = string.Empty;
	public string Issuer { get; init; } = string.Empty;
	public string Audience { get; init; } = string.Empty;
	public int AccessTokenTtlMinutes { get; init; } = 15;
	public int RefreshTokenTtlDays { get; init; } = 7;

	public static bool HasValidSecret(JwtOptions options) =>
		!string.IsNullOrWhiteSpace(options.Secret)
		&& options.Secret.Length >= 32
		&& !options.Secret.Contains(PlaceholderSecret, StringComparison.OrdinalIgnoreCase)
		&& !options.Secret.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase);
}
