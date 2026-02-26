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
    }
}
