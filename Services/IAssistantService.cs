using CrmWebApi.Common;
using CrmWebApi.DTOs.Assistant;

namespace CrmWebApi.Services;

public interface IAssistantService
{
	public IAsyncEnumerable<ChatStreamEvent> StreamAsync(
		int usrId,
		ChatRequest request,
		CancellationToken ct
	);

	public Task<Result<IReadOnlyList<ConversationSummaryResponse>>> ListConversationsAsync(
		int usrId,
		CancellationToken ct
	);

	public Task<Result<ConversationDetailsResponse>> GetConversationAsync(
		int usrId,
		long conversationId,
		CancellationToken ct
	);

	public Task<Result> DeleteConversationAsync(int usrId, long conversationId, CancellationToken ct);

	public Task<Result<object>> ConfirmActionAsync(int usrId, string actionId, CancellationToken ct);
}
