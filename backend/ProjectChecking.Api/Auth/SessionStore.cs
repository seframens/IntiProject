using System;
using System.Collections.Concurrent;
using ProjectChecking.Api.Models;

namespace ProjectChecking.Api.Auth;

public interface ISessionStore
{
    Session Create(int userId, string role);
    Session? Get(string token);
    void Remove(string token);
}

public class InMemorySessionStore : ISessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, Session> _sessions = new(StringComparer.Ordinal);

    public Session Create(int userId, string role)
    {
        var session = new Session
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Role = role,
            ExpiresAt = DateTime.UtcNow.Add(SessionLifetime),
        };
        _sessions[session.Token] = session;
        return session;
    }

    public Session? Get(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        if (!_sessions.TryGetValue(token, out var session))
        {
            return null;
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _sessions.TryRemove(token, out _);
            return null;
        }

        return session;
    }

    public void Remove(string token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _sessions.TryRemove(token, out _);
        }
    }
}
