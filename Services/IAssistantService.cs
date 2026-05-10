using CrmWebApi.Common;
using CrmWebApi.DTOs.Assistant;

namespace CrmWebApi.Services;

public interface IAssistantService
{
	IAsyncEnumerable<ChatStreamEvent> StreamAsync(
		int usrId,
		ChatRequest request,
		CancellationToken ct
	);

	Task<Result<IReadOnlyList<ConversationSummaryResponse>>> ListConversationsAsync(
		int usrId,
		CancellationToken ct
	);

	Task<Result<ConversationDetailsResponse>> GetConversationAsync(
		int usrId,
		long conversationId,
		CancellationToken ct
	);

	Task<Result> DeleteConversationAsync(int usrId, long conversationId, CancellationToken ct);

	Task<Result<object>> ConfirmActionAsync(int usrId, string actionId, CancellationToken ct);
}
