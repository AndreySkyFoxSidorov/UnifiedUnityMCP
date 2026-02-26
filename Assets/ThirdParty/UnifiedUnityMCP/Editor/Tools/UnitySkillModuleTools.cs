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

    internal static partial class UnitySkillModuleTools
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

        
    }
}

