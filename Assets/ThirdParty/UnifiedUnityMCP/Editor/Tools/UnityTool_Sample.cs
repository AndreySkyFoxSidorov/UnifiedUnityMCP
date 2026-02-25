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
    }
}
