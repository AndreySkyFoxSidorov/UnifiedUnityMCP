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
        private static UnityModuleTool CreateHistoryModule()
                {
                    var docs = CreateDocs("history_undo", "history_redo", "history_get_current", "bridge");
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_history"),
                        ["history_undo"] = Forward("unity_editor", raw =>
                        {
                            var payload = new JSONObject();
                            payload["action"] = "editor_undo";
                            return payload;
                        }),
                        ["history_redo"] = Forward("unity_editor", raw =>
                        {
                            var payload = new JSONObject();
                            payload["action"] = "editor_redo";
                            return payload;
                        }),
                        ["history_get_current"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var result = new JSONObject();
                                result["currentGroup"] = Undo.GetCurrentGroup();
                                result["currentGroupName"] = Undo.GetCurrentGroupName();
                                SendResult(sendResponse, result);
                            });
                        }
                    };
        
                    return new UnityModuleTool("unity_history", "Undo/redo history module.", docs, handlers, "History stack details are limited by Unity public Undo APIs.");
                }
    }
}
