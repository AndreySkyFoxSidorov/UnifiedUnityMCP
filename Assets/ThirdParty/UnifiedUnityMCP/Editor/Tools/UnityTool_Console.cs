using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Mcp.Editor.Tools
{
    internal static partial class UnitySkillModuleTools
    {
        private static UnityModuleTool CreateConsoleModule()
                {
                    var docs = CreateDocs("console_start_capture", "console_stop_capture", "console_get_logs", "console_clear", "console_log", "bridge");
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_console"),
                        ["console_get_logs"] = Forward("unity_console_read", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            if (payload.HasKey("limit") && !payload.HasKey("maxLines"))
                            {
                                payload["maxLines"] = payload["limit"];
                            }
                            return payload;
                        }),
                        ["console_log"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var payload = ExtractArguments(arguments);
                                string message = payload.GetString("message");
                                string type = payload.GetString("type", "Log").ToLowerInvariant();
        
                                if (type == "warning")
                                {
                                    Debug.LogWarning(message);
                                }
                                else if (type == "error" || type == "exception")
                                {
                                    Debug.LogError(message);
                                }
                                else
                                {
                                    Debug.Log(message);
                                }
        
                                SendText(sendResponse, "Console log written.");
                            });
                        },
                        ["console_clear"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                try
                                {
                                    var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
                                    var clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
                                    if (clearMethod == null)
                                    {
                                        sendError("Failed to resolve UnityEditor.LogEntries.Clear().");
                                        return;
                                    }
        
                                    clearMethod.Invoke(null, null);
                                    SendText(sendResponse, "Console cleared.");
                                }
                                catch (Exception e)
                                {
                                    sendError($"console_clear failed: {e.Message}");
                                }
                            });
                        }
                    };
        
                    return new UnityModuleTool("unity_console", "Console capture and log operations.", docs, handlers, "Streaming capture lifecycle (start/stop) is represented as direct read/clear in this implementation.");
                }
    }
}
