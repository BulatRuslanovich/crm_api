namespace CrmWebApi.Options;

public sealed class CacheOptions
{
	public const string SectionName = "Cache";

	public string? RedisConnectionString { get; init; }
}
