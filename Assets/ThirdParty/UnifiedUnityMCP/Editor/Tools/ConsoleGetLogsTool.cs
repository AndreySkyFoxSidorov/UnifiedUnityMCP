using System;
using System.Reflection;
using System.Text;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class ConsoleGetLogsTool : ITool
    {
        public string Name => "unity_console_read";
        public string Description => "Reads Unity Editor Console logs. specify maxLines to limit output.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["maxLines"] = McpMessages.CreateIntegerProperty("Maximum number of lines to return (default 50)");
                return McpMessages.CreateToolSchema(Name, Description, props);
            }
        }

        private static bool _initialized;
        private static bool _isSupported;
        private static MethodInfo _getCountMethod;
        private static MethodInfo _startGettingEntriesMethod;
        private static MethodInfo _getEntryInternalMethod;
        private static MethodInfo _endGettingEntriesMethod;
        private static object _logEntryInstance;
        private static FieldInfo _messageField;

        private static void TryInit()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null) return;

                _getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                _startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                _getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                _endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                var logEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
                if (logEntryType != null)
                {
                    _logEntryInstance = Activator.CreateInstance(logEntryType);
                    _messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                }

                if (_getCountMethod != null && _startGettingEntriesMethod != null && _getEntryInternalMethod != null && _endGettingEntriesMethod != null && _logEntryInstance != null)
                {
                    _isSupported = true;
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "ConsoleGetLogsTool.TryInit");
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            TryInit();
            if (!_isSupported)
            {
                sendResponse(McpMessages.CreateToolResult("Unsupported on this Unity version (reflection targets not found)."));
                return;
            }

            int maxLines = arguments.GetInt("maxLines", 50);

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    int count = (int)_getCountMethod.Invoke(null, null);
                    _startGettingEntriesMethod.Invoke(null, null);

                    int startIdx = Math.Max(0, count - maxLines);
                    var sb = new StringBuilder();
                    sb.AppendLine($"--- Outputting last {Math.Min(count, maxLines)} of {count} logs ---");

                    for (int i = startIdx; i < count; i++)
                    {
                        object[] getEntryArgs = new object[] { i, _logEntryInstance };
                        _getEntryInternalMethod.Invoke(null, getEntryArgs);
                        string message = _messageField?.GetValue(_logEntryInstance) as string;
                        if (!string.IsNullOrEmpty(message))
                        {
                            sb.AppendLine($"[{i}] {message.Replace("\n", " ").Replace("\r", "")}");
                        }
                    }

                    _endGettingEntriesMethod.Invoke(null, null);
                    sendResponse(McpMessages.CreateToolResult(sb.ToString()));
                }
                catch (Exception e)
                {
                    Logging.LogException(e, "ConsoleGetLogsTool");
                    sendError($"Failed to read console logs: {e.Message}");
                }
            });
        }
    }
}
