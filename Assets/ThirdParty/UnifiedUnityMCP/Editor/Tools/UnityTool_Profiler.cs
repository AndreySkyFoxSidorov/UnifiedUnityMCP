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
    }
}
