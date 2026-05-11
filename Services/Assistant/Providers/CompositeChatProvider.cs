using System.Runtime.CompilerServices;
using CrmWebApi.Options;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Services.Assistant.Providers;

public sealed class CompositeChatProvider(
	OllamaProvider local,
	OpenAiCompatibleProvider cloud,
	IOptions<AssistantOptions> options,
	ILogger<CompositeChatProvider> logger
) : IChatProvider
{
	public string Name => "composite";

	public async IAsyncEnumerable<ChatProviderEvent> StreamAsync(
		IReadOnlyList<ChatHistoryMessage> history,
		IReadOnlyList<ToolDefinition> tools,
		[EnumeratorCancellation] CancellationToken ct
	)
	{
		var (primary, fallback) = ResolveOrder();

		var yielded = false;
		Exception? primaryError = null;

		var enumerator = primary.StreamAsync(history, tools, ct).GetAsyncEnumerator(ct);
		try
		{
			while (true)
			{
				bool moved;
				try
				{
					moved = await enumerator.MoveNextAsync();
				}
				catch (Exception ex)
				{
					primaryError = ex;
					break;
				}

				if (!moved) break;
				yielded = true;
				yield return enumerator.Current;
			}
		}
		finally
		{
			await enumerator.DisposeAsync();
		}

		if (primaryError is null) yield break;

		if (yielded || fallback is null)
		{
			throw primaryError;
		}

		logger.LogWarning(
			primaryError,
			"Primary provider {Primary} failed before first event, falling back to {Fallback}",
			primary.Name,
			fallback.Name
		);

		await foreach (var ev in fallback.StreamAsync(history, tools, ct))
			yield return ev;
	}

	private (IChatProvider Primary, IChatProvider? Fallback) ResolveOrder()
	{
		var mode = (options.Value.Provider ?? string.Empty).Trim().ToLowerInvariant();
		return mode switch
		{
			AssistantProvider.Cloud => (cloud, null),
			AssistantProvider.CloudFallback => (cloud, local),
			AssistantProvider.LocalFallback => (local, cloud),
			_ => (local, null),
		};
	}
}
