namespace CrmWebApi.Options;

public sealed class AssistantOptions
{
	public const string SectionName = "Assistant";

	public bool Enabled { get; init; }
	public string UiGuidePath { get; init; } = string.Empty;
	public CloudSection Cloud { get; init; } = new();
	public LimitsSection Limits { get; init; } = new();

	public sealed class CloudSection
	{
		public string BaseUrl { get; init; } = "https://llms.dotpoin.com/v1";
		public string Model { get; init; } = "deepseek-chat";
		public string ApiKey { get; init; } = string.Empty;
		public bool Stream { get; init; }
	}

	public sealed class LimitsSection
	{
		public int MaxHistoryMessages { get; init; } = 20;
		public int MaxToolIterations { get; init; } = 5;
		public int MaxUserMessageChars { get; init; } = 4000;
	}
}
