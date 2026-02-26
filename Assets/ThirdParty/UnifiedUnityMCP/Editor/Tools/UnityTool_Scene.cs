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
    }
}
