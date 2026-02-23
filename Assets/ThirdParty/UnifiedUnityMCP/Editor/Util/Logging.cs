using System;
using UnityEngine;

namespace Mcp.Editor.Util
{
    public static class Logging
    {
        public static void Log(string message)
        {
            Debug.Log($"[MCP] {message}");
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[MCP] {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"[MCP] {message}");
        }
        
        public static void LogException(Exception e, string context = "")
        {
            string prefix = string.IsNullOrEmpty(context) ? "[MCP]" : $"[MCP] {context}:";
            Debug.LogError($"{prefix} {e.Message}\n{e.StackTrace}");
        }
    }
}
