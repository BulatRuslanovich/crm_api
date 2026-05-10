namespace CrmWebApi.Services.Assistant.Providers;

public interface IChatProvider
{
	string Name { get; }

	IAsyncEnumerable<ChatProviderEvent> StreamAsync(
		IReadOnlyList<ChatHistoryMessage> history,
		IReadOnlyList<ToolDefinition> tools,
		CancellationToken ct
	);
}
