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
    }
}
