using System.Text.Json;
using System.Text.RegularExpressions;
using CrmWebApi.Options;
using Microsoft.Extensions.Options;

namespace CrmWebApi.Services.Assistant.Tools;

public sealed partial class SearchUiHelpTool(IOptions<AssistantOptions> options) : IAssistantTool
{
	private readonly AssistantOptions _opts = options.Value;

	public string Name => "search_ui_help";

	public string Description =>
		"Search the CRM frontend navigation and workflow guide. Use for questions about screens, buttons, routes, tabs, and how to do something in the UI.";

	public string ParametersJsonSchema => """
		{
		  "type": "object",
		  "properties": {
		    "query": { "type": "string", "description": "The user's UI/navigation question in their original words" },
		    "limit": { "type": "integer", "minimum": 1, "maximum": 5, "default": 3 }
		  },
		  "required": ["query"]
		}
		""";

	public Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, CancellationToken ct)
	{
		var query = arguments.TryGetProperty("query", out var q) ? q.GetString() ?? string.Empty : string.Empty;
		var limit = arguments.TryGetProperty("limit", out var l) && l.ValueKind == JsonValueKind.Number
			? Math.Clamp(l.GetInt32(), 1, 5)
			: 3;

		if (string.IsNullOrWhiteSpace(_opts.UiGuidePath) || !File.Exists(_opts.UiGuidePath))
		{
			return Task.FromResult(ToolExecutionResult.Error("UI guide is not configured or not found."));
		}

		var guide = File.ReadAllText(_opts.UiGuidePath);
		var chunks = SplitMarkdown(guide);
		var queryTerms = Tokenize(query).ToHashSet(StringComparer.OrdinalIgnoreCase);

		var matches = chunks
			.Select(chunk => new
			{
				chunk.Title,
				chunk.Content,
				Score = Score(chunk, queryTerms),
			})
			.Where(x => x.Score > 0)
			.OrderByDescending(x => x.Score)
			.ThenBy(x => x.Content.Length)
			.Take(limit)
			.Select(x => new
			{
				title = x.Title,
				content = TrimForTool(x.Content),
			})
			.ToList();

		if (matches.Count == 0)
		{
			matches = chunks
				.Take(limit)
				.Select(x => new { title = x.Title, content = TrimForTool(x.Content) })
				.ToList();
		}

		return Task.FromResult(ToolExecutionResult.Ok(new { items = matches }));
	}

	private static int Score(UiHelpChunk chunk, HashSet<string> queryTerms)
	{
		if (queryTerms.Count == 0)
			return 0;

		var titleTerms = Tokenize(chunk.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var contentTerms = Tokenize(chunk.Content).ToHashSet(StringComparer.OrdinalIgnoreCase);

		var score = 0;
		foreach (var term in queryTerms)
		{
			if (titleTerms.Contains(term))
				score += 4;
			if (contentTerms.Contains(term))
				score += 1;
		}

		return score;
	}

	private static List<UiHelpChunk> SplitMarkdown(string markdown)
	{
		var chunks = new List<UiHelpChunk>();
		var currentTitle = "Общее";
		var current = new List<string>();

		foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
		{
			if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("### ", StringComparison.Ordinal))
			{
				AddChunk(chunks, currentTitle, current);
				currentTitle = line.TrimStart('#', ' ');
				current = [line];
				continue;
			}

			current.Add(line);
		}

		AddChunk(chunks, currentTitle, current);
		return chunks;
	}

	private static void AddChunk(List<UiHelpChunk> chunks, string title, List<string> lines)
	{
		var content = string.Join('\n', lines).Trim();
		if (!string.IsNullOrWhiteSpace(content))
			chunks.Add(new UiHelpChunk(title, content));
	}

	private static IEnumerable<string> Tokenize(string text)
	{
		foreach (Match match in WordRegex().Matches(text.ToLowerInvariant()))
		{
			var value = match.Value;
			if (value.Length >= 3)
				yield return value;
		}
	}

	private static string TrimForTool(string content)
	{
		const int maxChars = 1600;
		return content.Length <= maxChars ? content : content[..maxChars] + "...";
	}

	[GeneratedRegex(@"[\p{L}\p{N}/]+", RegexOptions.Compiled)]
	private static partial Regex WordRegex();

	private sealed record UiHelpChunk(string Title, string Content);
}
