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
    }
}
