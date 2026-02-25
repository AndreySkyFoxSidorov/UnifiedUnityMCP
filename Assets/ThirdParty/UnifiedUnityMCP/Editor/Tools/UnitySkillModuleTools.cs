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
    internal delegate void ModuleActionHandler(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError);

    internal sealed class UnityModuleTool : ITool
    {
        private readonly string _name;
        private readonly string _description;
        private readonly string _guidance;
        private readonly Dictionary<string, string> _documentedActions;
        private readonly Dictionary<string, ModuleActionHandler> _handlers;

        public string Name => _name;
        public string Description => _description;

        public UnityModuleTool(
            string name,
            string description,
            Dictionary<string, string> documentedActions,
            Dictionary<string, ModuleActionHandler> handlers,
            string guidance)
        {
            _name = name;
            _description = description;
            _documentedActions = documentedActions ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _handlers = handlers ?? new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase);
            _guidance = string.IsNullOrEmpty(guidance)
                ? "Use action='bridge' with a core unified tool for advanced scenarios."
                : guidance;
        }

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Module action. Use 'list_actions' to inspect capabilities.");
                props["tool"] = McpMessages.CreateStringProperty("Used by action='bridge'. Target core tool name to call.");

                var argsProp = new JSONObject();
                argsProp["type"] = "object";
                argsProp["description"] = "Arguments payload forwarded to the selected action/bridge target.";
                props["arguments"] = argsProp;

                return McpMessages.CreateToolSchema(Name, Description, props);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            var args = arguments ?? new JSONObject();
            string action = args.GetString("action", "list_actions");
            if (string.IsNullOrEmpty(action))
            {
                action = "list_actions";
            }

            action = action.Trim().ToLowerInvariant();

            if (action == "list_actions" || action == "help" || action == "capabilities")
            {
                SendInfo(sendResponse);
                return;
            }

            if (_handlers.TryGetValue(action, out var handler))
            {
                MainThreadDispatcher.InvokeAsync(() =>
                {
                    handler(args, sendResponse, sendError);
                });
                return;
            }

            if (_documentedActions.TryGetValue(action, out var actionDescription))
            {
                var payload = new JSONObject();
                payload["module"] = _name;
                payload["action"] = action;
                payload["status"] = "not_implemented";
                payload["description"] = actionDescription;
                payload["guidance"] = _guidance;
                sendResponse(McpMessages.CreateToolResult(payload.ToString()));
                return;
            }

            sendError($"Unknown action '{action}' for module '{_name}'. Use action='list_actions'.");
        }

        private void SendInfo(Action<JSONObject> sendResponse)
        {
            var payload = new JSONObject();
            payload["module"] = _name;
            payload["description"] = _description;
            payload["guidance"] = _guidance;

            var actions = new JSONArray();
            foreach (var kv in _documentedActions.OrderBy(k => k.Key))
            {
                var item = new JSONObject();
                item["name"] = kv.Key;
                item["description"] = kv.Value;
                item["implemented"] = _handlers.ContainsKey(kv.Key);
                actions.Add(item);
            }

            if (!_documentedActions.ContainsKey("bridge"))
            {
                var bridge = new JSONObject();
                bridge["name"] = "bridge";
                bridge["description"] = "Call an existing unified core tool through this module.";
                bridge["implemented"] = _handlers.ContainsKey("bridge");
                actions.Add(bridge);
            }

            payload["actions"] = actions;
            sendResponse(McpMessages.CreateToolResult(payload.ToString()));
        }
    }

    internal static class UnitySkillModuleTools
    {
        private sealed class SceneBookmark
        {
            public Vector3 Pivot;
            public Quaternion Rotation;
            public float Size;
            public bool Orthographic;
        }

        private static readonly Dictionary<string, SceneBookmark> _bookmarks = new Dictionary<string, SceneBookmark>(StringComparer.OrdinalIgnoreCase);

        public static List<ITool> CreateAll()
        {
            return new List<ITool>
            {
                CreateAnimatorModule(),
                CreateAssetModule(),
                CreateBookmarkModule(),
                CreateCameraModule(),
                CreateCinemachineModule(),
                CreateCleanerModule(),
                CreateComponentModule(),
                CreateConsoleModule(),
                CreateDebugModule(),
                CreateEditorModule(),
                CreateEventModule(),
                CreateGameObjectModule(),
                CreateHistoryModule(),
                CreateImporterModule(),
                CreateLightModule(),
                CreateMaterialModule(),
                CreateNavMeshModule(),
                CreateOptimizationModule(),
                CreatePackageModule(),
                CreatePerceptionModule(),
                CreatePhysicsModule(),
                CreatePrefabModule(),
                CreateProfilerModule(),
                CreateProjectModule(),
                CreateSampleModule(),
                CreateSceneModule(),
                CreateScriptModule(),
                CreateScriptableObjectModule(),
                CreateShaderModule(),
                CreateSmartModule(),
                CreateTerrainModule(),
                CreateTestModule(),
                CreateTimelineModule(),
                CreateUiModule(),
                CreateValidationModule(),
                CreateWorkflowModule()
            };
        }

        private static UnityModuleTool CreateBridgeOnlyModule(string name, string description, string guidance, params string[] documentedActions)
        {
            var docs = CreateDocs(documentedActions);
            docs["bridge"] = "Call an existing unified core tool through this module.";

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler(name)
            };

            return new UnityModuleTool(name, description, docs, handlers, guidance);
        }

        private static Dictionary<string, string> CreateDocs(params string[] actions)
        {
            var docs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var action in actions)
            {
                if (!string.IsNullOrEmpty(action))
                {
                    docs[action.ToLowerInvariant()] = action;
                }
            }

            return docs;
        }

        private static ModuleActionHandler CreateBridgeHandler(string moduleName)
        {
            return (arguments, sendResponse, sendError) =>
            {
                string targetTool = arguments.GetString("tool");
                if (string.IsNullOrEmpty(targetTool))
                {
                    sendError("Missing 'tool' for bridge action.");
                    return;
                }

                if (string.Equals(targetTool, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    sendError("Bridge target cannot be the same module tool.");
                    return;
                }

                if (!ToolRegistry.TryGetTool(targetTool, out var target))
                {
                    sendError($"Bridge target tool not found: {targetTool}");
                    return;
                }

                target.Execute(ExtractArguments(arguments), sendResponse, sendError);
            };
        }

        private static ModuleActionHandler InvokeOnObject(string methodName, string defaultComponentType = null)
        {
            return (arguments, sendResponse, sendError) =>
            {
                var payload = ExtractArguments(arguments);

                int instanceId = payload.GetInt("instanceId", 0);
                if (instanceId == 0)
                {
                    string name = payload.GetString("name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        var go = FindSceneObjectByName(name);
                        if (go != null)
                        {
                            instanceId = go.GetInstanceID();
                        }
                    }
                }

                if (instanceId == 0)
                {
                    sendError("Missing valid target object. Provide 'instanceId' or resolvable 'name'.");
                    return;
                }

                var invokePayload = new JSONObject();
                invokePayload["action"] = "invoke";
                invokePayload["instanceId"] = instanceId;
                invokePayload["property"] = methodName;

                if (payload.HasKey("args"))
                {
                    invokePayload["args"] = payload["args"];
                }

                if (payload.HasKey("componentType"))
                {
                    invokePayload["componentType"] = payload["componentType"];
                }
                else if (!string.IsNullOrEmpty(defaultComponentType))
                {
                    invokePayload["componentType"] = defaultComponentType;
                }

                if (!ToolRegistry.TryGetTool("unity_component_property", out var targetTool))
                {
                    sendError("Target core tool not found: unity_component_property");
                    return;
                }

                targetTool.Execute(invokePayload, sendResponse, sendError);
            };
        }

        private static ModuleActionHandler Forward(string toolName, Func<JSONObject, JSONObject> mapper = null)
        {
            return (arguments, sendResponse, sendError) =>
            {
                if (!ToolRegistry.TryGetTool(toolName, out var target))
                {
                    sendError($"Target core tool not found: {toolName}");
                    return;
                }

                var payload = mapper != null ? mapper(arguments) : ExtractArguments(arguments);
                target.Execute(payload, sendResponse, sendError);
            };
        }

        private static JSONObject ExtractArguments(JSONObject raw)
        {
            if (raw == null)
            {
                return new JSONObject();
            }

            var nested = raw.GetObject("arguments");
            if (nested != null)
            {
                return nested;
            }

            var extracted = new JSONObject();
            foreach (var kv in raw)
            {
                if (kv.Key == "action" || kv.Key == "tool")
                {
                    continue;
                }

                extracted[kv.Key] = kv.Value;
            }

            return extracted;
        }

        private static JSONObject ExtractArgumentsWithAction(JSONObject raw, string actionName)
        {
            var payload = ExtractArguments(raw);
            payload["action"] = actionName;
            return payload;
        }

        private static void SendResult(Action<JSONObject> sendResponse, JSONObject payload)
        {
            sendResponse(McpMessages.CreateToolResult(payload.ToString()));
        }

        private static void SendText(Action<JSONObject> sendResponse, string text)
        {
            sendResponse(McpMessages.CreateToolResult(text));
        }

        private static GameObject FindSceneObjectByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var all = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in all)
            {
                if (go == null)
                {
                    continue;
                }

                if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(go.scene.name) && string.Equals(go.name, name, StringComparison.Ordinal))
                {
                    return go;
                }
            }

            return null;
        }

        private static UnityModuleTool CreateAnimatorModule()
        {
            var docs = CreateDocs(
                "animator_create_controller",
                "animator_add_parameter",
                "animator_get_parameters",
                "animator_set_parameter",
                "animator_play",
                "animator_get_info",
                "animator_assign_controller",
                "animator_list_states",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_animator"),
                ["animator_set_parameter"] = InvokeOnObject("SetFloat", "Animator"),
                ["animator_play"] = InvokeOnObject("Play", "Animator")
            };

            return new UnityModuleTool(
                "unity_animator",
                "Animator workflows and controller operations.",
                docs,
                handlers,
                "Animator flows can be orchestrated through unity_component_property (including invoke) and unity_component_manage.");
        }

        private static UnityModuleTool CreateAssetModule()
        {
            var docs = CreateDocs(
                "asset_import",
                "asset_import_batch",
                "asset_delete",
                "asset_delete_batch",
                "asset_move",
                "asset_move_batch",
                "asset_duplicate",
                "asset_find",
                "asset_create_folder",
                "asset_refresh",
                "asset_get_info",
                "asset_reimport",
                "asset_reimport_batch",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_asset"),
                ["asset_find"] = Forward("unity_asset_manage", raw => ExtractArgumentsWithAction(raw, "find")),
                ["asset_refresh"] = Forward("unity_asset_manage", raw => ExtractArgumentsWithAction(raw, "refresh")),
                ["asset_create_folder"] = Forward("unity_asset_create", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "folder";
                    if (!payload.HasKey("path") && payload.HasKey("folderPath"))
                    {
                        payload["path"] = payload["folderPath"];
                    }
                    return payload;
                }),
                ["asset_get_info"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var payload = ExtractArguments(arguments);
                            string path = payload.GetString("path");
                            if (string.IsNullOrEmpty(path))
                            {
                                sendError("Missing 'path' for asset_get_info.");
                                return;
                            }

                            var result = new JSONObject();
                            result["path"] = path;
                            result["exists"] = AssetDatabase.LoadMainAssetAtPath(path) != null;
                            result["guid"] = AssetDatabase.AssetPathToGUID(path);
                            result["type"] = AssetDatabase.GetMainAssetTypeAtPath(path)?.FullName ?? "Unknown";
                            SendResult(sendResponse, result);
                        }
                        catch (Exception e)
                        {
                            sendError($"asset_get_info failed: {e.Message}");
                        }
                    });
                }
            };

            return new UnityModuleTool(
                "unity_asset",
                "Asset import, move, query, and refresh workflows.",
                docs,
                handlers,
                "Advanced file IO and import settings can be done through unity_asset_meta and unity_asset_manage.");
        }

        private static UnityModuleTool CreateBookmarkModule()
        {
            var docs = CreateDocs("bookmark_set", "bookmark_goto", "bookmark_list", "bookmark_delete", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_bookmark"),
                ["bookmark_set"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        string name = payload.GetString("name");
                        if (string.IsNullOrEmpty(name))
                        {
                            sendError("Missing 'name' for bookmark_set.");
                            return;
                        }

                        var view = SceneView.lastActiveSceneView;
                        if (view == null)
                        {
                            sendError("No active SceneView found.");
                            return;
                        }

                        _bookmarks[name] = new SceneBookmark
                        {
                            Pivot = view.pivot,
                            Rotation = view.rotation,
                            Size = view.size,
                            Orthographic = view.orthographic
                        };

                        var result = new JSONObject();
                        result["status"] = "saved";
                        result["name"] = name;
                        SendResult(sendResponse, result);
                    });
                },
                ["bookmark_goto"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        string name = payload.GetString("name");
                        if (string.IsNullOrEmpty(name))
                        {
                            sendError("Missing 'name' for bookmark_goto.");
                            return;
                        }

                        if (!_bookmarks.TryGetValue(name, out var bookmark))
                        {
                            sendError($"Bookmark not found: {name}");
                            return;
                        }

                        var view = SceneView.lastActiveSceneView;
                        if (view == null)
                        {
                            sendError("No active SceneView found.");
                            return;
                        }

                        view.pivot = bookmark.Pivot;
                        view.rotation = bookmark.Rotation;
                        view.size = bookmark.Size;
                        view.orthographic = bookmark.Orthographic;
                        view.Repaint();

                        var result = new JSONObject();
                        result["status"] = "moved";
                        result["name"] = name;
                        SendResult(sendResponse, result);
                    });
                },
                ["bookmark_list"] = (arguments, sendResponse, sendError) =>
                {
                    var result = new JSONObject();
                    var arr = new JSONArray();
                    foreach (var key in _bookmarks.Keys.OrderBy(k => k))
                    {
                        arr.Add(key);
                    }
                    result["count"] = _bookmarks.Count;
                    result["bookmarks"] = arr;
                    SendResult(sendResponse, result);
                },
                ["bookmark_delete"] = (arguments, sendResponse, sendError) =>
                {
                    var payload = ExtractArguments(arguments);
                    string name = payload.GetString("name");
                    if (string.IsNullOrEmpty(name))
                    {
                        sendError("Missing 'name' for bookmark_delete.");
                        return;
                    }

                    bool removed = _bookmarks.Remove(name);
                    var result = new JSONObject();
                    result["status"] = removed ? "deleted" : "not_found";
                    result["name"] = name;
                    SendResult(sendResponse, result);
                }
            };

            return new UnityModuleTool("unity_bookmark", "Scene view bookmarks.", docs, handlers, "Bookmarks are stored in-memory for the current editor session.");
        }

        private static UnityModuleTool CreateCameraModule()
        {
            var docs = CreateDocs("camera_align_view_to_object", "camera_get_info", "camera_set_transform", "camera_look_at", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_camera"),
                ["camera_get_info"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var view = SceneView.lastActiveSceneView;
                        if (view == null)
                        {
                            sendError("No active SceneView found.");
                            return;
                        }

                        var result = new JSONObject();
                        result["pivot"] = new JSONObject { ["x"] = view.pivot.x, ["y"] = view.pivot.y, ["z"] = view.pivot.z };
                        var euler = view.rotation.eulerAngles;
                        result["rotation"] = new JSONObject { ["x"] = euler.x, ["y"] = euler.y, ["z"] = euler.z };
                        result["size"] = view.size;
                        result["orthographic"] = view.orthographic;
                        SendResult(sendResponse, result);
                    });
                },
                ["camera_set_transform"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        var view = SceneView.lastActiveSceneView;
                        if (view == null)
                        {
                            sendError("No active SceneView found.");
                            return;
                        }

                        var pos = new Vector3(payload["posX"].AsFloat, payload["posY"].AsFloat, payload["posZ"].AsFloat);
                        var rot = Quaternion.Euler(payload["rotX"].AsFloat, payload["rotY"].AsFloat, payload["rotZ"].AsFloat);
                        bool instant = payload.GetBool("instant", true);
                        float size = payload.HasKey("size") ? payload["size"].AsFloat : view.size;

                        if (instant)
                        {
                            view.LookAtDirect(pos, rot, size);
                        }
                        else
                        {
                            view.LookAt(pos, rot, size, false);
                        }

                        var result = new JSONObject();
                        result["status"] = "ok";
                        SendResult(sendResponse, result);
                    });
                },
                ["camera_look_at"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        var view = SceneView.lastActiveSceneView;
                        if (view == null)
                        {
                            sendError("No active SceneView found.");
                            return;
                        }

                        var point = new Vector3(payload["x"].AsFloat, payload["y"].AsFloat, payload["z"].AsFloat);
                        view.LookAt(point);
                        var result = new JSONObject();
                        result["status"] = "ok";
                        SendResult(sendResponse, result);
                    });
                },
                ["camera_align_view_to_object"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        string objectName = payload.GetString("objectName");
                        var go = FindSceneObjectByName(objectName);
                        if (go == null)
                        {
                            sendError($"Object not found: {objectName}");
                            return;
                        }

                        var view = SceneView.lastActiveSceneView;
                        if (view == null)
                        {
                            sendError("No active SceneView found.");
                            return;
                        }

                        view.LookAt(go.transform.position, go.transform.rotation, view.size, true);
                        var result = new JSONObject();
                        result["status"] = "ok";
                        result["objectName"] = objectName;
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_camera", "SceneView camera controls.", docs, handlers, "Camera actions target Unity Editor SceneView.");
        }

        private static UnityModuleTool CreateCinemachineModule()
        {
            return CreateBridgeOnlyModule(
                "unity_cinemachine",
                "Cinemachine orchestration module.",
                "Cinemachine-specific helpers can be driven through component/property actions and package checks.",
                "cinemachine_create_vcam",
                "cinemachine_inspect_vcam",
                "cinemachine_set_vcam_property",
                "cinemachine_set_targets",
                "cinemachine_set_component",
                "cinemachine_add_component",
                "cinemachine_set_lens",
                "cinemachine_list_components",
                "cinemachine_impulse_generate",
                "cinemachine_get_brain_info",
                "cinemachine_create_target_group",
                "cinemachine_target_group_add_member",
                "cinemachine_target_group_remove_member",
                "cinemachine_set_spline",
                "cinemachine_add_extension",
                "cinemachine_remove_extension",
                "cinemachine_set_active",
                "cinemachine_create_mixing_camera",
                "cinemachine_mixing_camera_set_weight",
                "cinemachine_create_clear_shot",
                "cinemachine_create_state_driven_camera",
                "cinemachine_state_driven_camera_add_instruction",
                "cinemachine_set_noise");
        }

        private static UnityModuleTool CreateCleanerModule()
        {
            return CreateBridgeOnlyModule(
                "unity_cleaner",
                "Project cleanup and diagnostics module.",
                "Use unity_validation for core checks and asset tools for explicit cleanup commands.",
                "cleaner_find_unused_assets",
                "cleaner_find_duplicates",
                "cleaner_find_missing_references",
                "cleaner_delete_assets",
                "cleaner_get_asset_usage");
        }

        private static UnityModuleTool CreateComponentModule()
        {
            var docs = CreateDocs(
                "component_add",
                "component_remove",
                "component_list",
                "component_set_property",
                "component_get_properties",
                "component_add_batch",
                "component_remove_batch",
                "component_set_property_batch",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_component"),
                ["component_add"] = Forward("unity_component_manage", raw => ExtractArgumentsWithAction(raw, "add")),
                ["component_remove"] = Forward("unity_component_manage", raw => ExtractArgumentsWithAction(raw, "remove")),
                ["component_list"] = Forward("unity_component_manage", raw => ExtractArgumentsWithAction(raw, "list")),
                ["component_set_property"] = Forward("unity_component_property", raw => ExtractArgumentsWithAction(raw, "set")),
                ["component_get_properties"] = Forward("unity_component_property", raw => ExtractArgumentsWithAction(raw, "dump"))
            };

            return new UnityModuleTool("unity_component", "Component add/remove/introspection module.", docs, handlers, "Batch operations can be orchestrated by repeated calls from MCP client side.");
        }

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

        private static UnityModuleTool CreateEventModule()
        {
            return CreateBridgeOnlyModule(
                "unity_event",
                "UnityEvent inspection and editing module.",
                "Use bridge + component/property reflection for event data access.",
                "event_get_listeners",
                "event_add_listener",
                "event_remove_listener",
                "event_invoke");
        }

        private static UnityModuleTool CreateGameObjectModule()
        {
            var docs = CreateDocs(
                "gameobject_create",
                "gameobject_delete",
                "gameobject_duplicate",
                "gameobject_rename",
                "gameobject_find",
                "gameobject_get_info",
                "gameobject_set_transform",
                "gameobject_set_parent",
                "gameobject_set_active",
                "gameobject_create_batch",
                "gameobject_delete_batch",
                "gameobject_duplicate_batch",
                "gameobject_rename_batch",
                "gameobject_set_transform_batch",
                "gameobject_set_active_batch",
                "gameobject_set_parent_batch",
                "gameobject_set_layer_batch",
                "gameobject_set_tag_batch",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_gameobject"),
                ["gameobject_find"] = Forward("unity_gameobject_manage", raw => ExtractArgumentsWithAction(raw, "find")),
                ["gameobject_create"] = Forward("unity_gameobject_manage", raw => ExtractArgumentsWithAction(raw, "create")),
                ["gameobject_delete"] = Forward("unity_gameobject_manage", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "destroy";
                    return payload;
                }),
                ["gameobject_set_transform"] = Forward("unity_component_property", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "set";

                    string targetProperty = payload.GetString("propertyName");
                    if (string.IsNullOrEmpty(targetProperty))
                    {
                        bool hasPosition = payload.HasKey("posX") || payload.HasKey("posY") || payload.HasKey("posZ");
                        bool hasScale = payload.HasKey("scaleX") || payload.HasKey("scaleY") || payload.HasKey("scaleZ");
                        bool hasRotation = payload.HasKey("rotX") || payload.HasKey("rotY") || payload.HasKey("rotZ");

                        if (hasScale)
                        {
                            targetProperty = "localScale";
                        }
                        else if (hasRotation)
                        {
                            targetProperty = "eulerAngles";
                        }
                        else if (hasPosition)
                        {
                            targetProperty = "position";
                        }
                        else
                        {
                            targetProperty = "position";
                        }
                    }

                    var value = new JSONObject();

                    if (targetProperty == "localScale")
                    {
                        value["x"] = payload.HasKey("scaleX") ? payload["scaleX"].AsFloat : 1f;
                        value["y"] = payload.HasKey("scaleY") ? payload["scaleY"].AsFloat : 1f;
                        value["z"] = payload.HasKey("scaleZ") ? payload["scaleZ"].AsFloat : 1f;
                    }
                    else if (targetProperty == "eulerAngles")
                    {
                        value["x"] = payload.HasKey("rotX") ? payload["rotX"].AsFloat : 0f;
                        value["y"] = payload.HasKey("rotY") ? payload["rotY"].AsFloat : 0f;
                        value["z"] = payload.HasKey("rotZ") ? payload["rotZ"].AsFloat : 0f;
                    }
                    else
                    {
                        targetProperty = "position";
                        value["x"] = payload.HasKey("posX") ? payload["posX"].AsFloat : 0f;
                        value["y"] = payload.HasKey("posY") ? payload["posY"].AsFloat : 0f;
                        value["z"] = payload.HasKey("posZ") ? payload["posZ"].AsFloat : 0f;
                    }

                    payload["property"] = targetProperty;
                    payload["value"] = value;
                    return payload;
                }),
                ["gameobject_set_active"] = (arguments, sendResponse, sendError) =>
                {
                    var payload = ExtractArguments(arguments);
                    int instanceId = payload.GetInt("instanceId", 0);

                    if (payload.HasKey("name") && !payload.HasKey("instanceId"))
                    {
                        var go = FindSceneObjectByName(payload["name"].Value);
                        if (go != null)
                        {
                            instanceId = go.GetInstanceID();
                        }
                    }

                    if (instanceId == 0)
                    {
                        sendError("Missing valid target object. Provide 'instanceId' or resolvable 'name'.");
                        return;
                    }

#pragma warning disable CS0618
                    var goObj = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
                    if (goObj == null)
                    {
                        sendError($"GameObject with instance ID {instanceId} not found.");
                        return;
                    }

                    bool active = payload.GetBool("active", true);
                    Undo.RecordObject(goObj, "Set Active");
                    goObj.SetActive(active);
                    EditorUtility.SetDirty(goObj);

                    var result = new JSONObject();
                    result["instanceId"] = instanceId;
                    result["active"] = goObj.activeSelf;
                    SendResult(sendResponse, result);
                }
            };

            return new UnityModuleTool("unity_gameobject", "GameObject creation and query module.", docs, handlers, "For transform/tag/layer batch updates use component/property reflection with instanceIds.");
        }

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

        private static UnityModuleTool CreateImporterModule()
        {
            return CreateBridgeOnlyModule(
                "unity_importer",
                "Importer settings module.",
                "Use unity_asset_meta for importer property dump/get/set via SerializedProperty paths.",
                "texture_get_settings",
                "texture_set_settings",
                "texture_set_settings_batch",
                "texture_set_import_settings",
                "audio_get_settings",
                "audio_set_settings",
                "audio_set_settings_batch",
                "model_get_settings",
                "model_set_settings",
                "model_set_settings_batch",
                "model_set_import_settings");
        }

        private static UnityModuleTool CreateLightModule()
        {
            return CreateBridgeOnlyModule(
                "unity_light",
                "Lighting creation and tuning module.",
                "Use unity_gameobject + unity_component + unity_component_property for precise light setup.",
                "light_create",
                "light_set_properties",
                "light_set_properties_batch",
                "light_set_enabled",
                "light_set_enabled_batch",
                "light_get_info",
                "light_find_all");
        }

        private static UnityModuleTool CreateMaterialModule()
        {
            return CreateBridgeOnlyModule(
                "unity_material",
                "Material and shader property module.",
                "Use unity_asset_create + unity_component_property for assignment and property updates.",
                "material_create",
                "material_create_batch",
                "material_assign",
                "material_assign_batch",
                "material_set_color",
                "material_set_colors_batch",
                "material_set_emission",
                "material_set_emission_batch",
                "material_set_texture",
                "material_set_float",
                "material_set_int",
                "material_set_keyword",
                "material_get_properties",
                "material_get_keywords",
                "material_duplicate",
                "material_set_shader",
                "material_set_vector",
                "material_set_texture_offset",
                "material_set_texture_scale",
                "material_set_render_queue",
                "material_set_gi_flags");
        }

        private static UnityModuleTool CreateNavMeshModule()
        {
            var docs = CreateDocs("navmesh_bake", "navmesh_clear", "navmesh_calculate_path", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_navmesh"),
                ["navmesh_calculate_path"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        var start = new Vector3(payload["startX"].AsFloat, payload["startY"].AsFloat, payload["startZ"].AsFloat);
                        var end = new Vector3(payload["endX"].AsFloat, payload["endY"].AsFloat, payload["endZ"].AsFloat);
                        int areaMask = payload.GetInt("areaMask", NavMesh.AllAreas);

                        var navPath = new NavMeshPath();
                        bool found = NavMesh.CalculatePath(start, end, areaMask, navPath);

                        float distance = 0f;
                        if (navPath.corners != null && navPath.corners.Length > 1)
                        {
                            for (int i = 1; i < navPath.corners.Length; i++)
                            {
                                distance += Vector3.Distance(navPath.corners[i - 1], navPath.corners[i]);
                            }
                        }

                        var corners = new JSONArray();
                        if (navPath.corners != null)
                        {
                            foreach (var c in navPath.corners)
                            {
                                corners.Add(new JSONObject { ["x"] = c.x, ["y"] = c.y, ["z"] = c.z });
                            }
                        }

                        var result = new JSONObject();
                        result["hit"] = found;
                        result["status"] = navPath.status.ToString();
                        result["distance"] = distance;
                        result["corners"] = corners;
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_navmesh", "NavMesh path query module.", docs, handlers, "Bake/clear operations depend on project NavMesh authoring workflow and are not forced automatically.");
        }

        private static UnityModuleTool CreateOptimizationModule()
        {
            return CreateBridgeOnlyModule(
                "unity_optimization",
                "Optimization workflows module.",
                "Combine importer and validation modules to enforce optimization policies.",
                "optimize_textures",
                "optimize_mesh_compression");
        }

        private static UnityModuleTool CreatePackageModule()
        {
            var docs = CreateDocs("package_list", "package_check", "package_install", "package_remove", "package_refresh", "package_install_cinemachine", "package_get_cinemachine_status", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_package"),
                ["package_list"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var dependencies = ReadManifestDependencies();
                            var arr = new JSONArray();
                            foreach (var kv in dependencies.OrderBy(k => k.Key))
                            {
                                var pkg = new JSONObject();
                                pkg["name"] = kv.Key;
                                pkg["version"] = kv.Value;
                                arr.Add(pkg);
                            }

                            var result = new JSONObject();
                            result["count"] = dependencies.Count;
                            result["packages"] = arr;
                            SendResult(sendResponse, result);
                        }
                        catch (Exception e)
                        {
                            sendError($"package_list failed: {e.Message}");
                        }
                    });
                },
                ["package_check"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var payload = ExtractArguments(arguments);
                            string packageId = payload.GetString("packageId");
                            if (string.IsNullOrEmpty(packageId))
                            {
                                sendError("Missing 'packageId'.");
                                return;
                            }

                            var dependencies = ReadManifestDependencies();
                            var result = new JSONObject();
                            result["packageId"] = packageId;
                            result["installed"] = dependencies.ContainsKey(packageId);
                            result["version"] = dependencies.ContainsKey(packageId) ? dependencies[packageId] : "";
                            SendResult(sendResponse, result);
                        }
                        catch (Exception e)
                        {
                            sendError($"package_check failed: {e.Message}");
                        }
                    });
                },
                ["package_get_cinemachine_status"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var dependencies = ReadManifestDependencies();
                            var result = new JSONObject();

                            var cm = new JSONObject();
                            cm["installed"] = dependencies.ContainsKey("com.unity.cinemachine");
                            cm["version"] = dependencies.ContainsKey("com.unity.cinemachine") ? dependencies["com.unity.cinemachine"] : "";
                            result["cinemachine"] = cm;

                            var splines = new JSONObject();
                            splines["installed"] = dependencies.ContainsKey("com.unity.splines");
                            splines["version"] = dependencies.ContainsKey("com.unity.splines") ? dependencies["com.unity.splines"] : "";
                            result["splines"] = splines;

                            SendResult(sendResponse, result);
                        }
                        catch (Exception e)
                        {
                            sendError($"package_get_cinemachine_status failed: {e.Message}");
                        }
                    });
                }
            };

            return new UnityModuleTool("unity_package", "Package manifest/module checks.", docs, handlers, "Install/remove operations should be executed with explicit PackageManager workflows to handle async state.");
        }

        private static Dictionary<string, string> ReadManifestDependencies()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string manifestPath = Path.Combine(projectRoot ?? string.Empty, "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var json = JSON.Parse(File.ReadAllText(manifestPath)) as JSONObject;
            var deps = json?["dependencies"] as JSONObject;
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (deps == null)
            {
                return result;
            }

            foreach (var kv in deps)
            {
                result[kv.Key] = kv.Value != null ? kv.Value.Value : string.Empty;
            }

            return result;
        }

        private static UnityModuleTool CreatePerceptionModule()
        {
            var docs = CreateDocs(
                "scene_summarize",
                "hierarchy_describe",
                "script_analyze",
                "scene_spatial_query",
                "scene_materials",
                "scene_context",
                "scene_export_report",
                "scene_dependency_analyze",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_perception"),
                ["scene_summarize"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var scene = SceneManager.GetActiveScene();
                        var roots = scene.GetRootGameObjects();

                        int total = 0;
                        int active = 0;
                        int maxDepth = 0;
                        int lights = 0;
                        int cameras = 0;
                        int canvases = 0;

                        foreach (var root in roots)
                        {
                            CountHierarchy(root.transform, 1, ref total, ref active, ref maxDepth, ref lights, ref cameras, ref canvases);
                        }

                        var stats = new JSONObject();
                        stats["totalObjects"] = total;
                        stats["activeObjects"] = active;
                        stats["rootObjects"] = roots.Length;
                        stats["maxHierarchyDepth"] = maxDepth;
                        stats["lights"] = lights;
                        stats["cameras"] = cameras;
                        stats["canvases"] = canvases;

                        var result = new JSONObject();
                        result["sceneName"] = scene.name;
                        result["stats"] = stats;
                        SendResult(sendResponse, result);
                    });
                },
                ["hierarchy_describe"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        int maxDepth = payload.GetInt("maxDepth", 5);
                        bool includeInactive = payload.GetBool("includeInactive", false);

                        var scene = SceneManager.GetActiveScene();
                        var sb = new StringBuilder();
                        sb.AppendLine($"Scene: {scene.name}");
                        sb.AppendLine("----------------------------------------");

                        foreach (var root in scene.GetRootGameObjects())
                        {
                            AppendHierarchy(sb, root.transform, 0, maxDepth, includeInactive);
                        }

                        var result = new JSONObject();
                        result["description"] = sb.ToString();
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_perception", "Scene introspection and reporting module.", docs, handlers, "Deep dependency/export reports can be layered on top of scene_summarize and hierarchy_describe outputs.");
        }

        private static void CountHierarchy(
            Transform node,
            int depth,
            ref int total,
            ref int active,
            ref int maxDepth,
            ref int lights,
            ref int cameras,
            ref int canvases)
        {
            total++;
            if (node.gameObject.activeInHierarchy)
            {
                active++;
            }

            if (depth > maxDepth)
            {
                maxDepth = depth;
            }

            if (node.GetComponent<Light>() != null)
            {
                lights++;
            }

            if (node.GetComponent<Camera>() != null)
            {
                cameras++;
            }

            if (node.GetComponent<Canvas>() != null)
            {
                canvases++;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                CountHierarchy(node.GetChild(i), depth + 1, ref total, ref active, ref maxDepth, ref lights, ref cameras, ref canvases);
            }
        }

        private static void AppendHierarchy(StringBuilder sb, Transform node, int depth, int maxDepth, bool includeInactive)
        {
            if (!includeInactive && !node.gameObject.activeInHierarchy)
            {
                return;
            }

            if (depth > maxDepth)
            {
                return;
            }

            sb.Append(new string(' ', depth * 2));
            sb.Append("- ");
            sb.AppendLine(node.name);

            for (int i = 0; i < node.childCount; i++)
            {
                AppendHierarchy(sb, node.GetChild(i), depth + 1, maxDepth, includeInactive);
            }
        }

        private static UnityModuleTool CreatePhysicsModule()
        {
            var docs = CreateDocs("physics_raycast", "physics_check_overlap", "physics_get_gravity", "physics_set_gravity", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_physics"),
                ["physics_get_gravity"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var g = Physics.gravity;
                        var result = new JSONObject();
                        result["x"] = g.x;
                        result["y"] = g.y;
                        result["z"] = g.z;
                        SendResult(sendResponse, result);
                    });
                },
                ["physics_set_gravity"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        Physics.gravity = new Vector3(payload["x"].AsFloat, payload["y"].AsFloat, payload["z"].AsFloat);
                        var g = Physics.gravity;
                        var result = new JSONObject();
                        result["x"] = g.x;
                        result["y"] = g.y;
                        result["z"] = g.z;
                        SendResult(sendResponse, result);
                    });
                },
                ["physics_raycast"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        var origin = new Vector3(payload["originX"].AsFloat, payload["originY"].AsFloat, payload["originZ"].AsFloat);
                        var direction = new Vector3(payload["dirX"].AsFloat, payload["dirY"].AsFloat, payload["dirZ"].AsFloat);
                        float maxDistance = payload.HasKey("maxDistance") ? payload["maxDistance"].AsFloat : 1000f;
                        int mask = payload.GetInt("layerMask", -1);

                        var result = new JSONObject();
                        if (Physics.Raycast(origin, direction, out var hit, maxDistance, mask))
                        {
                            result["hit"] = true;
                            result["distance"] = hit.distance;
                            result["collider"] = hit.collider != null ? hit.collider.name : "";
                            result["point"] = new JSONObject { ["x"] = hit.point.x, ["y"] = hit.point.y, ["z"] = hit.point.z };
                        }
                        else
                        {
                            result["hit"] = false;
                        }

                        SendResult(sendResponse, result);
                    });
                },
                ["physics_check_overlap"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        var center = new Vector3(payload["x"].AsFloat, payload["y"].AsFloat, payload["z"].AsFloat);
                        float radius = payload["radius"].AsFloat;
                        int mask = payload.GetInt("layerMask", -1);

                        var colliders = Physics.OverlapSphere(center, radius, mask);
                        var arr = new JSONArray();
                        foreach (var col in colliders)
                        {
                            var item = new JSONObject();
                            item["name"] = col.name;
                            item["instanceId"] = col.GetInstanceID();
                            arr.Add(item);
                        }

                        var result = new JSONObject();
                        result["count"] = colliders.Length;
                        result["colliders"] = arr;
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_physics", "Physics helper module.", docs, handlers, "Physics queries operate on current scene colliders and project physics settings.");
        }

        private static UnityModuleTool CreatePrefabModule()
        {
            var docs = CreateDocs(
                "prefab_create",
                "prefab_instantiate",
                "prefab_instantiate_batch",
                "prefab_apply",
                "prefab_unpack",
                "prefab_get_overrides",
                "prefab_revert_overrides",
                "prefab_apply_overrides",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_prefab"),
                ["prefab_instantiate"] = Forward("unity_prefab_instantiate", raw =>
                {
                    var payload = ExtractArguments(raw);
                    if (!payload.HasKey("assetPath") && payload.HasKey("prefabPath"))
                    {
                        payload["assetPath"] = payload["prefabPath"];
                    }

                    return payload;
                })
            };

            return new UnityModuleTool("unity_prefab", "Prefab creation and instantiation module.", docs, handlers, "Advanced prefab override/apply flows can be added via dedicated prefab handlers.");
        }

        private static UnityModuleTool CreateProfilerModule()
        {
            var docs = CreateDocs("profiler_get_stats", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_profiler"),
                ["profiler_get_stats"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var result = new JSONObject();
                        result["fpsApprox"] = Time.deltaTime > 0.00001f ? (1.0f / Time.deltaTime) : 0.0f;
                        result["batches"] = UnityStats.batches;
                        result["setPassCalls"] = UnityStats.setPassCalls;
                        result["triangles"] = UnityStats.triangles;
                        result["vertices"] = UnityStats.vertices;
                        result["totalAllocatedMB"] = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                        result["totalReservedMB"] = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_profiler", "Performance statistics module.", docs, handlers, "Stats are sampled from editor/runtime counters and can fluctuate frame to frame.");
        }

        private static UnityModuleTool CreateProjectModule()
        {
            var docs = CreateDocs("project_get_info", "project_get_render_pipeline", "project_list_shaders", "project_get_quality_settings", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_project"),
                ["project_get_info"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var result = new JSONObject();
                        result["unityVersion"] = Application.unityVersion;
                        result["platform"] = Application.platform.ToString();
                        result["companyName"] = Application.companyName;
                        result["productName"] = Application.productName;
                        result["activeScene"] = SceneManager.GetActiveScene().path;
                        result["isPlaying"] = EditorApplication.isPlaying;
                        result["isCompiling"] = EditorApplication.isCompiling;
                        SendResult(sendResponse, result);
                    });
                },
                ["project_get_render_pipeline"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
                        var result = new JSONObject();
                        result["renderPipelineAsset"] = asset != null ? asset.name : "Built-in";
                        result["renderPipelineType"] = asset != null ? asset.GetType().FullName : "Built-in";
                        SendResult(sendResponse, result);
                    });
                },
                ["project_list_shaders"] = Forward("unity_asset_manage", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "find";
                    string filter = payload.GetString("filter", "");
                    payload["filter"] = string.IsNullOrEmpty(filter) ? "t:Shader" : $"t:Shader {filter}";
                    return payload;
                }),
                ["project_get_quality_settings"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var arr = new JSONArray();
                        var names = QualitySettings.names;
                        for (int i = 0; i < names.Length; i++)
                        {
                            var q = new JSONObject();
                            q["index"] = i;
                            q["name"] = names[i];
                            q["isCurrent"] = i == QualitySettings.GetQualityLevel();
                            arr.Add(q);
                        }

                        var result = new JSONObject();
                        result["qualityLevels"] = arr;
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_project", "Project metadata and settings module.", docs, handlers, "Use additional scene and asset modules for deeper project audits.");
        }

        private static UnityModuleTool CreateSampleModule()
        {
            var docs = CreateDocs(
                "create_cube",
                "create_sphere",
                "delete_object",
                "find_objects_by_name",
                "set_object_position",
                "set_object_rotation",
                "set_object_scale",
                "get_scene_info",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_sample"),
                ["create_cube"] = Forward("unity_gameobject_manage", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "create";
                    payload["primitiveType"] = "Cube";
                    if (!payload.HasKey("name")) payload["name"] = "Cube";
                    return payload;
                }),
                ["create_sphere"] = Forward("unity_gameobject_manage", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "create";
                    payload["primitiveType"] = "Sphere";
                    if (!payload.HasKey("name")) payload["name"] = "Sphere";
                    return payload;
                }),
                ["delete_object"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        string objectName = payload.GetString("objectName");
                        var go = FindSceneObjectByName(objectName);
                        if (go == null)
                        {
                            sendError($"Object not found: {objectName}");
                            return;
                        }

                        Undo.DestroyObjectImmediate(go);
                        SendText(sendResponse, "Object deleted.");
                    });
                },
                ["find_objects_by_name"] = Forward("unity_gameobject_manage", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "find";
                    if (!payload.HasKey("name") && payload.HasKey("nameContains"))
                    {
                        payload["name"] = payload["nameContains"];
                    }
                    return payload;
                }),
                ["set_object_position"] = Forward("unity_component_property", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "set";
                    payload["property"] = "position";
                    var value = new JSONObject();
                    value["x"] = payload["x"].AsFloat;
                    value["y"] = payload["y"].AsFloat;
                    value["z"] = payload["z"].AsFloat;
                    payload["value"] = value;
                    return payload;
                }),
                ["set_object_rotation"] = Forward("unity_component_property", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "set";
                    payload["property"] = "eulerAngles";
                    var value = new JSONObject();
                    value["x"] = payload["x"].AsFloat;
                    value["y"] = payload["y"].AsFloat;
                    value["z"] = payload["z"].AsFloat;
                    payload["value"] = value;
                    return payload;
                }),
                ["set_object_scale"] = Forward("unity_component_property", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "set";
                    payload["property"] = "localScale";
                    var value = new JSONObject();
                    value["x"] = payload["x"].AsFloat;
                    value["y"] = payload["y"].AsFloat;
                    value["z"] = payload["z"].AsFloat;
                    payload["value"] = value;
                    return payload;
                }),
                ["get_scene_info"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var scene = SceneManager.GetActiveScene();
                        var result = new JSONObject();
                        result["name"] = scene.name;
                        result["path"] = scene.path;
                        result["isDirty"] = scene.isDirty;
                        result["rootObjectCount"] = scene.rootCount;
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_sample", "Sample utility module.", docs, handlers, "Sample actions are intended for smoke tests and demos.");
        }

        private static UnityModuleTool CreateSceneModule()
        {
            var docs = CreateDocs(
                "scene_create",
                "scene_load",
                "scene_save",
                "scene_get_info",
                "scene_get_hierarchy",
                "scene_screenshot",
                "scene_get_loaded",
                "scene_unload",
                "scene_set_active",
                "scene_summarize",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_scene"),
                ["scene_create"] = Forward("unity_scene_manage", raw =>
                {
                    var payload = new JSONObject();
                    payload["action"] = "new";
                    return payload;
                }),
                ["scene_load"] = Forward("unity_scene_manage", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "open";
                    if (!payload.HasKey("path") && payload.HasKey("scenePath"))
                    {
                        payload["path"] = payload["scenePath"];
                    }
                    return payload;
                }),
                ["scene_save"] = Forward("unity_scene_manage", raw => ExtractArgumentsWithAction(raw, "save")),
                ["scene_get_info"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var scene = SceneManager.GetActiveScene();
                        var result = new JSONObject();
                        result["name"] = scene.name;
                        result["path"] = scene.path;
                        result["isDirty"] = scene.isDirty;
                        result["rootObjectCount"] = scene.rootCount;
                        SendResult(sendResponse, result);
                    });
                },
                ["scene_get_loaded"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var arr = new JSONArray();
                        for (int i = 0; i < SceneManager.sceneCount; i++)
                        {
                            var s = SceneManager.GetSceneAt(i);
                            var obj = new JSONObject();
                            obj["name"] = s.name;
                            obj["path"] = s.path;
                            obj["isLoaded"] = s.isLoaded;
                            obj["isActive"] = s == SceneManager.GetActiveScene();
                            arr.Add(obj);
                        }

                        var result = new JSONObject();
                        result["scenes"] = arr;
                        SendResult(sendResponse, result);
                    });
                },
                ["scene_summarize"] = Forward("unity_perception", raw =>
                {
                    var payload = ExtractArguments(raw);
                    payload["action"] = "scene_summarize";
                    return payload;
                })
            };

            return new UnityModuleTool("unity_scene", "Scene lifecycle module.", docs, handlers, "Hierarchy/screenshot/additive management can be expanded with dedicated scene handlers.");
        }

        private static UnityModuleTool CreateScriptModule()
        {
            return CreateBridgeOnlyModule(
                "unity_script",
                "C# script management module.",
                "Script file creation/editing can be handled by external file tools plus unity_asset_manage refresh.",
                "script_create",
                "script_create_batch",
                "script_read",
                "script_delete",
                "script_find_in_file",
                "script_append");
        }

        private static UnityModuleTool CreateScriptableObjectModule()
        {
            return CreateBridgeOnlyModule(
                "unity_scriptableobject",
                "ScriptableObject asset management module.",
                "Use bridge with asset and component tools to inspect and manipulate serialized values.",
                "scriptableobject_create",
                "scriptableobject_get",
                "scriptableobject_set",
                "scriptableobject_list_types",
                "scriptableobject_duplicate");
        }

        private static UnityModuleTool CreateShaderModule()
        {
            return CreateBridgeOnlyModule(
                "unity_shader",
                "Shader file and metadata module.",
                "Use project_list_shaders and asset operations for shader file lifecycle.",
                "shader_create",
                "shader_read",
                "shader_list",
                "shader_find",
                "shader_delete",
                "shader_get_properties");
        }

        private static UnityModuleTool CreateSmartModule()
        {
            return CreateBridgeOnlyModule(
                "unity_smart",
                "Smart query/layout/binding module.",
                "Compose smart flows by chaining gameobject/component/property actions with client-side filtering.",
                "smart_scene_query",
                "smart_scene_layout",
                "smart_reference_bind");
        }

        private static UnityModuleTool CreateTerrainModule()
        {
            return CreateBridgeOnlyModule(
                "unity_terrain",
                "Terrain editing and generation module.",
                "Terrain operations can be added incrementally via dedicated TerrainData handlers.",
                "terrain_create",
                "terrain_get_info",
                "terrain_get_height",
                "terrain_set_height",
                "terrain_set_heights_batch",
                "terrain_add_hill",
                "terrain_generate_perlin",
                "terrain_smooth",
                "terrain_flatten",
                "terrain_paint_texture");
        }

        private static UnityModuleTool CreateTestModule()
        {
            var docs = CreateDocs("test_list", "test_run", "test_get_result", "test_cancel", "bridge");
            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_test"),
                ["test_run"] = Forward("unity_test_run", raw =>
                {
                    var payload = ExtractArguments(raw);
                    if (payload.HasKey("testMode") && !payload.HasKey("mode"))
                    {
                        string mode = payload["testMode"].Value;
                        payload["mode"] = string.Equals(mode, "PlayMode", StringComparison.OrdinalIgnoreCase) ? "playmode" : "editmode";
                    }

                    if (!payload.HasKey("mode"))
                    {
                        payload["mode"] = "editmode";
                    }

                    return payload;
                })
            };

            return new UnityModuleTool("unity_test", "Unity Test Runner module.", docs, handlers, "Async job polling/cancel can be layered on top of unity_test_run results.");
        }

        private static UnityModuleTool CreateTimelineModule()
        {
            return CreateBridgeOnlyModule(
                "unity_timeline",
                "Timeline authoring module.",
                "Timeline-specific authoring can be integrated with PlayableDirector tools incrementally.",
                "timeline_create",
                "timeline_add_audio_track",
                "timeline_add_animation_track");
        }

        private static UnityModuleTool CreateUiModule()
        {
            return CreateBridgeOnlyModule(
                "unity_ui",
                "UGUI creation and layout module.",
                "UI workflows can be scripted through editor menu execution and component/property tools.",
                "ui_create_canvas",
                "ui_create_panel",
                "ui_create_button",
                "ui_create_text",
                "ui_create_image",
                "ui_create_inputfield",
                "ui_create_slider",
                "ui_create_toggle",
                "ui_set_text",
                "ui_find_all",
                "ui_set_rect",
                "ui_set_anchor",
                "ui_layout_children",
                "ui_align_selected",
                "ui_distribute_selected",
                "ui_create_batch");
        }

        private static UnityModuleTool CreateValidationModule()
        {
            var docs = CreateDocs(
                "validate_scene",
                "validate_find_missing_scripts",
                "validate_fix_missing_scripts",
                "validate_cleanup_empty_folders",
                "validate_find_unused_assets",
                "validate_texture_sizes",
                "validate_project_structure",
                "bridge");

            var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["bridge"] = CreateBridgeHandler("unity_validation"),
                ["validate_scene"] = Forward("unity_validation", raw =>
                {
                    var payload = new JSONObject();
                    payload["action"] = "validate_find_missing_scripts";
                    return payload;
                }),
                ["validate_find_missing_scripts"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var issues = new JSONArray();
                        int totalMissing = 0;

                        var all = Resources.FindObjectsOfTypeAll<GameObject>();
                        foreach (var go in all)
                        {
                            if (go == null || string.IsNullOrEmpty(go.scene.name))
                            {
                                continue;
                            }

                            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                            if (missing <= 0)
                            {
                                continue;
                            }

                            totalMissing += missing;
                            var entry = new JSONObject();
                            entry["name"] = go.name;
                            entry["path"] = GetGameObjectPath(go.transform);
                            entry["missingCount"] = missing;
                            issues.Add(entry);
                        }

                        var result = new JSONObject();
                        result["count"] = issues.Count;
                        result["missingScripts"] = totalMissing;
                        result["objectsWithMissingScripts"] = issues;
                        SendResult(sendResponse, result);
                    });
                },
                ["validate_fix_missing_scripts"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        bool dryRun = payload.GetBool("dryRun", true);

                        int total = 0;
                        var fixedObjects = new JSONArray();

                        var all = Resources.FindObjectsOfTypeAll<GameObject>();
                        foreach (var go in all)
                        {
                            if (go == null || string.IsNullOrEmpty(go.scene.name))
                            {
                                continue;
                            }

                            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                            if (missing <= 0)
                            {
                                continue;
                            }

                            int fixedCount = missing;
                            if (!dryRun)
                            {
                                fixedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                            }

                            total += fixedCount;
                            var entry = new JSONObject();
                            entry["name"] = go.name;
                            entry["fixed"] = fixedCount;
                            fixedObjects.Add(entry);
                        }

                        var result = new JSONObject();
                        result["dryRun"] = dryRun;
                        result["totalFixed"] = total;
                        result["fixedObjects"] = fixedObjects;
                        SendResult(sendResponse, result);
                    });
                },
                ["validate_texture_sizes"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        int maxSize = payload.GetInt("maxRecommendedSize", 2048);
                        int limit = payload.GetInt("limit", 50);

                        var guids = AssetDatabase.FindAssets("t:Texture2D");
                        var oversized = new JSONArray();
                        int checkedCount = 0;

                        foreach (var guid in guids)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                            if (tex == null)
                            {
                                continue;
                            }

                            checkedCount++;
                            if (tex.width <= maxSize && tex.height <= maxSize)
                            {
                                continue;
                            }

                            var item = new JSONObject();
                            item["path"] = path;
                            item["width"] = tex.width;
                            item["height"] = tex.height;
                            oversized.Add(item);

                            if (oversized.Count >= limit)
                            {
                                break;
                            }
                        }

                        var result = new JSONObject();
                        result["totalChecked"] = checkedCount;
                        result["oversizedCount"] = oversized.Count;
                        result["oversizedTextures"] = oversized;
                        SendResult(sendResponse, result);
                    });
                },
                ["validate_project_structure"] = (arguments, sendResponse, sendError) =>
                {
                    MainThreadDispatcher.InvokeAsync(() =>
                    {
                        var payload = ExtractArguments(arguments);
                        string rootPath = payload.GetString("rootPath", "Assets");
                        int maxDepth = payload.GetInt("maxDepth", 3);

                        string fullRoot = ResolveProjectPath(rootPath);
                        if (!Directory.Exists(fullRoot))
                        {
                            sendError($"Path does not exist: {rootPath}");
                            return;
                        }

                        var structure = BuildDirectoryTree(fullRoot, rootPath.Replace("\\", "/"), 0, maxDepth);
                        var result = new JSONObject();
                        result["rootPath"] = rootPath;
                        result["structure"] = structure;
                        SendResult(sendResponse, result);
                    });
                }
            };

            return new UnityModuleTool("unity_validation", "Project validation and cleanup module.", docs, handlers, "Unused-asset and destructive cleanup flows should be reviewed manually before applying deletes.");
        }

        private static string ResolveProjectPath(string assetLikePath)
        {
            if (string.IsNullOrEmpty(assetLikePath) || assetLikePath == "Assets")
            {
                return Application.dataPath;
            }

            string normalized = assetLikePath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            string suffix = normalized.Substring("Assets".Length).TrimStart('/');
            return Path.Combine(Application.dataPath, suffix);
        }

        private static JSONObject BuildDirectoryTree(string fullPath, string relativePath, int depth, int maxDepth)
        {
            var node = new JSONObject();
            node["path"] = relativePath;
            node["depth"] = depth;

            if (depth >= maxDepth)
            {
                return node;
            }

            var children = new JSONArray();
            foreach (var dir in Directory.GetDirectories(fullPath).OrderBy(d => d))
            {
                string name = Path.GetFileName(dir);
                string childRel = string.IsNullOrEmpty(relativePath) ? name : (relativePath + "/" + name);
                children.Add(BuildDirectoryTree(dir, childRel.Replace("\\", "/"), depth + 1, maxDepth));
            }

            node["children"] = children;
            return node;
        }

        private static string GetGameObjectPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            var current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private static UnityModuleTool CreateWorkflowModule()
        {
            return CreateBridgeOnlyModule(
                "unity_workflow",
                "Workflow history/session module.",
                "Use unity_history actions for editor undo/redo and track higher-level sessions in your MCP client.",
                "workflow_session_start",
                "workflow_session_end",
                "workflow_session_undo",
                "workflow_session_list",
                "workflow_session_status",
                "workflow_task_start",
                "workflow_task_end",
                "workflow_snapshot_object",
                "workflow_snapshot_created",
                "workflow_list",
                "workflow_undo_task",
                "workflow_redo_task",
                "workflow_undone_list",
                "workflow_revert_task",
                "workflow_delete_task");
        }
    }
}

