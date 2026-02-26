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
        private static UnityModuleTool CreateDebugModule()
                {
                    var docs = CreateDocs("debug_log", "debug_get_logs", "debug_get_errors", "debug_check_compilation", "debug_force_recompile", "debug_get_system_info", "bridge");
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_debug"),
                        ["debug_log"] = Forward("unity_console", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            payload["action"] = "console_log";
                            return payload;
                        }),
                        ["debug_get_logs"] = Forward("unity_console", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            payload["action"] = "console_get_logs";
                            return payload;
                        }),
                        ["debug_get_errors"] = Forward("unity_console_read", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            payload["maxLines"] = payload.GetInt("limit", 50);
                            return payload;
                        }),
                        ["debug_check_compilation"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var result = new JSONObject();
                                result["isCompiling"] = EditorApplication.isCompiling;
                                result["isUpdating"] = EditorApplication.isUpdating;
                                SendResult(sendResponse, result);
                            });
                        },
                        ["debug_force_recompile"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                CompilationPipeline.RequestScriptCompilation();
                                SendText(sendResponse, "Requested script recompilation.");
                            });
                        },
                        ["debug_get_system_info"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var result = new JSONObject();
                                result["unityVersion"] = Application.unityVersion;
                                result["platform"] = Application.platform.ToString();
                                result["operatingSystem"] = SystemInfo.operatingSystem;
                                result["processor"] = SystemInfo.processorType;
                                result["graphicsDevice"] = SystemInfo.graphicsDeviceName;
                                result["graphicsApi"] = SystemInfo.graphicsDeviceType.ToString();
                                result["systemMemoryMB"] = SystemInfo.systemMemorySize;
                                SendResult(sendResponse, result);
                            });
                        }
                    };
        
                    return new UnityModuleTool("unity_debug", "Debug and diagnostics helpers.", docs, handlers, "Use debug_get_logs + debug_check_compilation as baseline diagnostics flow.");
                }
    }
}
