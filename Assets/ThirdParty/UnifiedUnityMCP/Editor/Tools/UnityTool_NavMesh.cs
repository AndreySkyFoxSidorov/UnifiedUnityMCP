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
    }
}
