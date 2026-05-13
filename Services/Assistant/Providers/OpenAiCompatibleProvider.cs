using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using CrmWebApi.Options;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Services.Assistant.Providers;

public sealed class OpenAiCompatibleProvider(
	HttpClient http,
	IOptions<AssistantOptions> options,
	ILogger<OpenAiCompatibleProvider> logger
) : IChatProvider
{
	private readonly AssistantOptions _opts = options.Value;

	public string Name => "cloud";

	public async IAsyncEnumerable<ChatProviderEvent> StreamAsync(
		IReadOnlyList<ChatHistoryMessage> history,
		IReadOnlyList<ToolDefinition> tools,
		[EnumeratorCancellation] CancellationToken ct
	)
	{
		var payload = BuildPayload(history, tools);

		using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
		{
			Content = JsonContent.Create(payload),
		};

		using var resp = await http.SendAsync(
			req,
			_opts.Cloud.Stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
			ct
		);
		if (!resp.IsSuccessStatusCode)
		{
			var body = await resp.Content.ReadAsStringAsync(ct);
			logger.LogError("Cloud provider returned {Status}: {Body}", resp.StatusCode, body);
			throw new InvalidOperationException($"Cloud provider error {(int)resp.StatusCode}: {body}");
		}

		if (!_opts.Cloud.Stream)
		{
			var body = await resp.Content.ReadAsStringAsync(ct);
			var finished = ParseNonStreamingResponse(body);
			if (!string.IsNullOrEmpty(finished.FullText))
				yield return new ChatTokenEvent(finished.FullText);

			yield return finished;
			yield break;
		}

		await using var stream = await resp.Content.ReadAsStreamAsync(ct);
		using var reader = new StreamReader(stream, Encoding.UTF8);

		var fullText = new StringBuilder();
		var toolAcc = new ToolCallAccumulator();

		while (true)
		{
			ct.ThrowIfCancellationRequested();
			var line = await reader.ReadLineAsync(ct);
			if (line is null) break;
			if (string.IsNullOrEmpty(line)) continue;
			if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

			var data = line[5..].TrimStart();
			if (data == "[DONE]") break;

			JsonDocument doc;
			try { doc = JsonDocument.Parse(data); }
			catch (JsonException ex)
			{
				logger.LogWarning(ex, "Failed to parse provider line: {Line}", line);
				continue;
			}

			using (doc)
			{
				var root = doc.RootElement;
				if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array) continue;
				foreach (var choice in choices.EnumerateArray())
				{
					if (!choice.TryGetProperty("delta", out var delta)) continue;

					if (delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
					{
						var text = content.GetString();
						if (!string.IsNullOrEmpty(text))
						{
							fullText.Append(text);
							yield return new ChatTokenEvent(text);
						}
					}

					if (delta.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
					{
						toolAcc.Apply(toolCalls);
					}
				}
			}
		}

		var collected = toolAcc.Build();
		yield return new ChatFinishedEvent(fullText.ToString(), collected.Count == 0 ? null : collected);
	}

	private object BuildPayload(IReadOnlyList<ChatHistoryMessage> history, IReadOnlyList<ToolDefinition> tools)
	{
		var messages = history.Select(BuildMessage).ToList();

		return new
		{
			model = _opts.Cloud.Model,
			stream = _opts.Cloud.Stream,
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
	}

	private ChatFinishedEvent ParseNonStreamingResponse(string body)
	{
		try
		{
			using var doc = JsonDocument.Parse(body);
			var root = doc.RootElement;
			if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
				return new ChatFinishedEvent(string.Empty, null);

			var fullText = new StringBuilder();
			var toolCalls = new List<ChatToolCall>();

			foreach (var choice in choices.EnumerateArray())
			{
				if (!choice.TryGetProperty("message", out var message)) continue;

				if (message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
				{
					var text = content.GetString();
					if (!string.IsNullOrEmpty(text))
						fullText.Append(text);
				}

				if (message.TryGetProperty("tool_calls", out var calls) && calls.ValueKind == JsonValueKind.Array)
				{
					foreach (var call in calls.EnumerateArray())
					{
						if (!call.TryGetProperty("function", out var fn)) continue;
						var name = fn.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
							? n.GetString()
							: null;
						if (string.IsNullOrEmpty(name)) continue;

						var args = fn.TryGetProperty("arguments", out var a) && a.ValueKind == JsonValueKind.String
							? a.GetString() ?? "{}"
							: "{}";
						var id = call.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String
							? idEl.GetString()!
							: $"call_{Guid.NewGuid():N}";

						toolCalls.Add(new ChatToolCall(id, name, args));
					}
				}
			}

			return new ChatFinishedEvent(fullText.ToString(), toolCalls.Count == 0 ? null : toolCalls);
		}
		catch (JsonException ex)
		{
			logger.LogWarning(ex, "Failed to parse cloud provider response: {Body}", body);
			return new ChatFinishedEvent(string.Empty, null);
		}
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
						arguments = tc.ArgumentsJson,
					},
				}).ToArray(),
			};
		}

		return new { role = m.Role, content = m.Content };
	}

	private sealed class ToolCallAccumulator
	{
		private readonly SortedDictionary<int, Slot> _slots = [];

		public void Apply(JsonElement deltaToolCalls)
		{
			var idx = 0;
			foreach (var tc in deltaToolCalls.EnumerateArray())
			{
				var index = tc.TryGetProperty("index", out var idxEl) && idxEl.ValueKind == JsonValueKind.Number
					? idxEl.GetInt32() : idx;
				idx++;

				if (!_slots.TryGetValue(index, out var slot))
				{
					slot = new Slot();
					_slots[index] = slot;
				}

				if (tc.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
					slot.Id = id.GetString();

				if (tc.TryGetProperty("function", out var fn))
				{
					if (fn.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
						slot.Name = name.GetString();
					if (fn.TryGetProperty("arguments", out var args) && args.ValueKind == JsonValueKind.String)
						slot.Arguments.Append(args.GetString());
				}
			}
		}

		public List<ChatToolCall> Build() => _slots.Values
			.Where(s => !string.IsNullOrEmpty(s.Name))
			.Select(s => new ChatToolCall(
				string.IsNullOrEmpty(s.Id) ? $"call_{Guid.NewGuid():N}" : s.Id,
				s.Name!,
				s.Arguments.Length == 0 ? "{}" : s.Arguments.ToString()
			))
			.ToList();

		private sealed class Slot
		{
			public string? Id;
			public string? Name;
			public readonly StringBuilder Arguments = new();
		}
	}
}
