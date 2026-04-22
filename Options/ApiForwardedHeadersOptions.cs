namespace CrmWebApi.Options;

public class ApiForwardedHeadersOptions
{
	public const string SectionName = "ForwardedHeaders";

	public int ForwardLimit { get; init; } = 1;

	public string[] KnownProxies { get; init; } = ["127.0.0.1"];

	public string[] KnownNetworks { get; init; } = [];
}
