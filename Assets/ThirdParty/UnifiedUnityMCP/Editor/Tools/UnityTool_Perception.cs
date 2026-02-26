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
    }
}
