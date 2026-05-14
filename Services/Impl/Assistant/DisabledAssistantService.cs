using System.Runtime.CompilerServices;
using CrmWebApi.Common;
using CrmWebApi.DTOs.Assistant;

namespace CrmWebApi.Services.Impl.Assistant;

public sealed class DisabledAssistantService : IAssistantService
{
	private const string Message = "AI assistant is disabled.";

	public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(
		int usrId,
		ChatRequest request,
		[EnumeratorCancellation] CancellationToken ct
	)
	{
		await Task.CompletedTask;
		ct.ThrowIfCancellationRequested();
		yield return new ChatStreamEvent(ChatStreamEventType.Error, new ErrorPayload(Message));
		yield return new ChatStreamEvent(ChatStreamEventType.Done);
	}

	public Task<Result<IReadOnlyList<ConversationSummaryResponse>>> ListConversationsAsync(
		int usrId,
		CancellationToken ct
	) =>
		Task.FromResult<Result<IReadOnlyList<ConversationSummaryResponse>>>(Error.Unavailable(Message));

	public Task<Result<ConversationDetailsResponse>> GetConversationAsync(
		int usrId,
		long conversationId,
		CancellationToken ct
	) =>
		Task.FromResult<Result<ConversationDetailsResponse>>(Error.Unavailable(Message));

	public Task<Result> DeleteConversationAsync(int usrId, long conversationId, CancellationToken ct) =>
		Task.FromResult<Result>(Error.Unavailable(Message));

	public Task<Result<object>> ConfirmActionAsync(int usrId, string actionId, CancellationToken ct) =>
		Task.FromResult<Result<object>>(Error.Unavailable(Message));
}
