namespace CrmWebApi.Options;

public sealed class AssistantOptions
{
	public const string SectionName = "Assistant";

	public string Provider { get; init; } = AssistantProvider.Ollama;
	public string UiGuidePath { get; init; } = string.Empty;
	public OllamaSection Ollama { get; init; } = new();
	public CloudSection Cloud { get; init; } = new();
	public LimitsSection Limits { get; init; } = new();

	public sealed class OllamaSection
	{
		public string BaseUrl { get; init; } = "http://localhost:11434";
		public string Model { get; init; } = "qwen2.5:3b";
	}

	public sealed class CloudSection
	{
		public string BaseUrl { get; init; } = string.Empty;
		public string Model { get; init; } = string.Empty;
		public string ApiKey { get; init; } = string.Empty;
	}

	public sealed class LimitsSection
	{
		public int MaxHistoryMessages { get; init; } = 20;
		public int MaxToolIterations { get; init; } = 5;
		public int MaxUserMessageChars { get; init; } = 4000;
	}
}

public static class AssistantProvider
{
	public const string Ollama = "ollama";
	public const string Cloud = "cloud";
	public const string CloudFallback = "cloud-fallback";
	public const string LocalFallback = "local-fallback";

	public static bool RequiresCloudConfig(string provider) =>
		provider is Cloud or CloudFallback;

	public static bool UsesCloud(string provider) =>
		provider is Cloud or CloudFallback or LocalFallback;
}
