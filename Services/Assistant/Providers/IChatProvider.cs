namespace CrmWebApi.Services.Assistant.Providers;

public interface IChatProvider
{
	public string Name { get; }

	public IAsyncEnumerable<ChatProviderEvent> StreamAsync(
		IReadOnlyList<ChatHistoryMessage> history,
		IReadOnlyList<ToolDefinition> tools,
		CancellationToken ct
	);
}
