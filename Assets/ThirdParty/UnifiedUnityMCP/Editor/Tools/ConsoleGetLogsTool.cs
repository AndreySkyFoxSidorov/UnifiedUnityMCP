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
        public string Name => "unity.console.read";
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

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            int maxLines = arguments.GetInt("maxLines", 50);

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    // Access Unity's internal LogEntries using reflection
                    var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
                    if (logEntriesType == null)
                    {
                        sendError("Could not find UnityEditor.LogEntries type.");
                        return;
                    }

                    var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                    var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
                    var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                    var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

                    if (getCountMethod == null || startGettingEntriesMethod == null || getEntryInternalMethod == null || endGettingEntriesMethod == null)
                    {
                        sendError("Could not find required reflection methods on UnityEditor.LogEntries.");
                        return;
                    }

                    // LogEntry class
                    var logEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
                    var logEntryInstance = Activator.CreateInstance(logEntryType);
                    var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);

                    int count = (int)getCountMethod.Invoke(null, null);
                    startGettingEntriesMethod.Invoke(null, null);

                    int startIdx = Math.Max(0, count - maxLines);
                    var sb = new StringBuilder();
                    sb.AppendLine($"--- Outputting last {Math.Min(count, maxLines)} of {count} logs ---");

                    for (int i = startIdx; i < count; i++)
                    {
                        object[] getEntryArgs = new object[] { i, logEntryInstance };
                        getEntryInternalMethod.Invoke(null, getEntryArgs);
                        string message = messageField?.GetValue(logEntryInstance) as string;
                        if (!string.IsNullOrEmpty(message))
                        {
                            sb.AppendLine($"[{i}] {message.Replace("\n", " ").Replace("\r", "")}");
                        }
                    }

                    endGettingEntriesMethod.Invoke(null, null);
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
