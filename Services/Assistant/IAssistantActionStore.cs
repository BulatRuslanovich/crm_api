namespace CrmWebApi.Services.Assistant;

public sealed record PendingAction(
	string Id,
	int UsrId,
	string Tool,
	string PayloadJson,
	string Summary,
	DateTimeOffset ExpiresAt
);

public interface IAssistantActionStore
{
	public PendingAction Put(int usrId, string tool, string payloadJson, string summary);
	public PendingAction? Take(int usrId, string id);
}
