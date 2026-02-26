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
    }
}
