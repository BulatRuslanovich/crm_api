namespace CrmWebApi.DTOs.Assistant;

public sealed record ConversationSummaryResponse(
	long ConversationId,
	string? Title,
	DateTimeOffset CreatedAt,
	DateTimeOffset UpdatedAt
);

public sealed record ConversationMessageResponse(
	long MessageId,
	string Role,
	string Content,
	DateTimeOffset CreatedAt
);

public sealed record ConversationDetailsResponse(
	long ConversationId,
	string? Title,
	DateTimeOffset CreatedAt,
	IReadOnlyList<ConversationMessageResponse> Messages
);
