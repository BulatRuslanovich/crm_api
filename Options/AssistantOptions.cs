namespace CrmWebApi.Options;

public sealed class AssistantOptions
{
	public const string SectionName = "Assistant";

	public OllamaSection Ollama { get; init; } = new();
	public LimitsSection Limits { get; init; } = new();

	public sealed class OllamaSection
	{
		public string BaseUrl { get; init; } = "http://localhost:11434";
		public string Model { get; init; } = "qwen2.5:3b";
	}

	public sealed class LimitsSection
	{
		public int MaxHistoryMessages { get; init; } = 20;
		public int MaxToolIterations { get; init; } = 5;
		public int MaxUserMessageChars { get; init; } = 4000;
	}
}
