using System.Collections.Concurrent;

namespace ReceiptReader.Services.Messaging.ComposeWizard;

/// <summary>
/// Process-wide store of active compose-wizard sessions, keyed by Telegram user id.
/// </summary>
/// <remarks>
/// Sessions expire after 30 minutes without activity. Entries are not persisted across process restarts.
/// </remarks>
internal sealed class ComposeWizardSessionStore
{
    private static readonly TimeSpan IdleTtl = TimeSpan.FromMinutes(30);

    private readonly ConcurrentDictionary<long, ComposeWizardSession> _sessions = new();

    /// <summary>
    /// Returns the user's session if it exists and has not idle-expired.
    /// </summary>
    public bool TryGet(long telegramUserId, out ComposeWizardSession? session)
    {
        if (!_sessions.TryGetValue(telegramUserId, out session))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - session.LastActivityAt > IdleTtl)
        {
            _sessions.TryRemove(telegramUserId, out _);
            session = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the user has a non-expired session.
    /// </summary>
    public bool HasActiveSession(long telegramUserId) => TryGet(telegramUserId, out _);

    /// <summary>
    /// Replaces the user's session and refreshes <see cref="ComposeWizardSession.LastActivityAt"/>.
    /// </summary>
    public void Set(long telegramUserId, ComposeWizardSession session)
    {
        session.LastActivityAt = DateTimeOffset.UtcNow;
        _sessions[telegramUserId] = session;
    }

    /// <summary>
    /// Removes the user's session if present.
    /// </summary>
    /// <returns><see langword="true"/> if a session was removed.</returns>
    public bool Clear(long telegramUserId) => _sessions.TryRemove(telegramUserId, out _);
}
