using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CrmWebApi.Options;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Services.Assistant.Providers;

public sealed class OllamaProvider(HttpClient http, IOptions<AssistantOptions> options, ILogger<OllamaProvider> logger)
	: IChatProvider
{
	private readonly AssistantOptions _opts = options.Value;

	public string Name => "ollama";

	public async IAsyncEnumerable<ChatProviderEvent> StreamAsync(
		IReadOnlyList<ChatHistoryMessage> history,
		IReadOnlyList<ToolDefinition> tools,
		[EnumeratorCancellation] CancellationToken ct
	)
	{
		var payload = BuildPayload(history, tools);

		using var req = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
		{
			Content = JsonContent.Create(payload),
		};

		using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
		if (!resp.IsSuccessStatusCode)
		{
			var body = await resp.Content.ReadAsStringAsync(ct);
			logger.LogError("Ollama returned {Status}: {Body}", resp.StatusCode, body);
			throw new InvalidOperationException($"Ollama error {(int)resp.StatusCode}: {body}");
		}

		await using var stream = await resp.Content.ReadAsStreamAsync(ct);
		using var reader = new StreamReader(stream, Encoding.UTF8);

		var fullText = new StringBuilder();
		List<ChatToolCall>? collectedCalls = null;

		while (true)
		{
			ct.ThrowIfCancellationRequested();
			var line = await reader.ReadLineAsync(ct);
			if (line is null) break;
			if (string.IsNullOrWhiteSpace(line)) continue;

			JsonDocument doc;
			try
			{
				doc = JsonDocument.Parse(line);
			}
			catch (JsonException ex)
			{
				logger.LogWarning(ex, "Failed to parse Ollama line: {Line}", line);
				continue;
			}

			using (doc)
			{
				var root = doc.RootElement;

				if (root.TryGetProperty("message", out var message))
				{
					if (message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
					{
						var text = content.GetString();
						if (!string.IsNullOrEmpty(text))
						{
							fullText.Append(text);
							yield return new ChatTokenEvent(text);
						}
					}

					if (message.TryGetProperty("tool_calls", out var toolCalls)
						&& toolCalls.ValueKind == JsonValueKind.Array
						&& toolCalls.GetArrayLength() > 0)
					{
						collectedCalls ??= [];
						foreach (var tc in toolCalls.EnumerateArray())
						{
							if (!tc.TryGetProperty("function", out var fn)) continue;
							var name = fn.TryGetProperty("name", out var n) ? n.GetString() : null;
							if (string.IsNullOrEmpty(name)) continue;

							var argsJson = fn.TryGetProperty("arguments", out var args)
								? args.GetRawText()
								: "{}";

							var id = tc.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
								? idEl.GetString()!
								: $"call_{Guid.NewGuid():N}";

							collectedCalls.Add(new ChatToolCall(id, name, argsJson));
						}
					}
				}

				if (root.TryGetProperty("done", out var done) && done.ValueKind == JsonValueKind.True)
				{
					yield return new ChatFinishedEvent(fullText.ToString(), collectedCalls);
					yield break;
				}
			}
		}

		yield return new ChatFinishedEvent(fullText.ToString(), collectedCalls);
	}

	private object BuildPayload(
		IReadOnlyList<ChatHistoryMessage> history,
		IReadOnlyList<ToolDefinition> tools
	)
	{
		var messages = history.Select(BuildMessage).ToList();

		object payload = new
		{
			model = _opts.Ollama.Model,
			stream = true,
			messages,
			tools = tools.Count == 0
				? null
				: tools.Select(t => new
				{
					type = "function",
					function = new
					{
						name = t.Name,
						description = t.Description,
						parameters = JsonSerializer.Deserialize<JsonElement>(t.ParametersJsonSchema),
					},
				}).ToArray(),
		};

		return payload;
	}

	private static object BuildMessage(ChatHistoryMessage m)
	{
		if (m.Role == ChatRoles.Tool)
		{
			return new
			{
				role = "tool",
				content = m.Content,
				tool_call_id = m.ToolCallId,
			};
		}

		if (m.Role == ChatRoles.Assistant && m.ToolCalls is { Count: > 0 })
		{
			return new
			{
				role = "assistant",
				content = m.Content,
				tool_calls = m.ToolCalls.Select(tc => new
				{
					id = tc.Id,
					type = "function",
					function = new
					{
						name = tc.Name,
						arguments = JsonSerializer.Deserialize<JsonElement>(tc.ArgumentsJson),
					},
				}).ToArray(),
			};
		}

		return new { role = m.Role, content = m.Content };
	}
}
