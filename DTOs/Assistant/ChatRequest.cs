namespace CrmWebApi.DTOs.Assistant;

public sealed record ChatRequest(long? ConversationId, string Message);
