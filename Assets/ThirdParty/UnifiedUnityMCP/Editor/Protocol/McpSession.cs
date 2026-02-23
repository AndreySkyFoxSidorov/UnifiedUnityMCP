using System.Collections.Concurrent;
using System;

namespace Mcp.Editor.Protocol
{
    public static class McpSession
    {
        private static readonly ConcurrentDictionary<string, bool> _activeSessions = new ConcurrentDictionary<string, bool>();

        public static string CreateSession()
        {
            string sessionId = Guid.NewGuid().ToString();
            _activeSessions.TryAdd(sessionId, true);
            return sessionId;
        }

        public static bool IsValidSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return false;
            return _activeSessions.ContainsKey(sessionId);
        }

        public static void InvalidateSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) return;
            _activeSessions.TryRemove(sessionId, out _);
        }

        public static void ClearAll()
        {
            _activeSessions.Clear();
        }
    }
}
