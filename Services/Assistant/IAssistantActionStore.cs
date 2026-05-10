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
	PendingAction Put(int usrId, string tool, string payloadJson, string summary);
	PendingAction? Take(int usrId, string id);
}
