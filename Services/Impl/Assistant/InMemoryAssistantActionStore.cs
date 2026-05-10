using System.Collections.Concurrent;
using CrmWebApi.Services.Assistant;

namespace CrmWebApi.Services.Impl.Assistant;

public sealed class InMemoryAssistantActionStore : IAssistantActionStore
{
	private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);
	private readonly ConcurrentDictionary<string, PendingAction> _store = new();

	public PendingAction Put(int usrId, string tool, string payloadJson, string summary)
	{
		Cleanup();
		var id = "act_" + Guid.NewGuid().ToString("N")[..16];
		var action = new PendingAction(id, usrId, tool, payloadJson, summary, DateTimeOffset.UtcNow.Add(Ttl));
		_store[id] = action;
		return action;
	}

	public PendingAction? Take(int usrId, string id)
	{
		Cleanup();
		if (!_store.TryRemove(id, out var action)) return null;
		if (action.UsrId != usrId) return null;
		if (action.ExpiresAt < DateTimeOffset.UtcNow) return null;
		return action;
	}

	private void Cleanup()
	{
		var now = DateTimeOffset.UtcNow;
		foreach (var (key, action) in _store)
		{
			if (action.ExpiresAt < now)
				_store.TryRemove(key, out _);
		}
	}
}
