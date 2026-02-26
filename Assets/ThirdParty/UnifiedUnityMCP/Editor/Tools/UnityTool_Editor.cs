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
        private static UnityModuleTool CreateEditorModule()
                {
                    var docs = CreateDocs(
                        "editor_play",
                        "editor_stop",
                        "editor_pause",
                        "editor_select",
                        "editor_get_selection",
                        "editor_get_context",
                        "editor_undo",
                        "editor_redo",
                        "editor_get_state",
                        "editor_execute_menu",
                        "editor_get_tags",
                        "editor_get_layers",
                        "editor_set_pause_on_error",
                        "bridge");
        
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_editor"),
                        ["editor_get_state"] = Forward("unity_editor_state"),
                        ["editor_get_selection"] = Forward("unity_selection_get"),
                        ["editor_execute_menu"] = Forward("unity_editor_execute_menu"),
                        ["editor_play"] = Forward("unity_editor_set_state", raw =>
                        {
                            var payload = new JSONObject();
                            payload["state"] = "play";
                            return payload;
                        }),
                        ["editor_stop"] = Forward("unity_editor_set_state", raw =>
                        {
                            var payload = new JSONObject();
                            payload["state"] = "stop";
                            return payload;
                        }),
                        ["editor_pause"] = Forward("unity_editor_set_state", raw =>
                        {
                            var payload = new JSONObject();
                            payload["state"] = "pause";
                            return payload;
                        }),
                        ["editor_undo"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                Undo.PerformUndo();
                                SendText(sendResponse, "Undo performed.");
                            });
                        },
                        ["editor_redo"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                Undo.PerformRedo();
                                SendText(sendResponse, "Redo performed.");
                            });
                        },
                        ["editor_get_tags"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var tags = new JSONArray();
                                foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
                                {
                                    tags.Add(tag);
                                }
        
                                var result = new JSONObject();
                                result["tags"] = tags;
                                SendResult(sendResponse, result);
                            });
                        },
                        ["editor_get_layers"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var layers = new JSONArray();
                                var layerNames = UnityEditorInternal.InternalEditorUtility.layers;
                                for (int i = 0; i < layerNames.Length; i++)
                                {
                                    var entry = new JSONObject();
                                    entry["name"] = layerNames[i];
                                    entry["index"] = LayerMask.NameToLayer(layerNames[i]);
                                    layers.Add(entry);
                                }
        
                                var result = new JSONObject();
                                result["layers"] = layers;
                                SendResult(sendResponse, result);
                            });
                        }
                    };
        
                    return new UnityModuleTool("unity_editor", "Unity Editor state/control module.", docs, handlers, "Selection set and advanced context can be bridged to existing tools and workflow commands.");
                }
    }
}
