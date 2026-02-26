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
        private static UnityModuleTool CreateAssetModule()
                {
                    var docs = CreateDocs(
                        "asset_import",
                        "asset_import_batch",
                        "asset_delete",
                        "asset_delete_batch",
                        "asset_move",
                        "asset_move_batch",
                        "asset_duplicate",
                        "asset_find",
                        "asset_create_folder",
                        "asset_refresh",
                        "asset_get_info",
                        "asset_reimport",
                        "asset_reimport_batch",
                        "bridge");
        
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_asset"),
                        ["asset_find"] = Forward("unity_asset_manage", raw => ExtractArgumentsWithAction(raw, "find")),
                        ["asset_refresh"] = Forward("unity_asset_manage", raw => ExtractArgumentsWithAction(raw, "refresh")),
                        ["asset_create_folder"] = Forward("unity_asset_create", raw =>
                        {
                            var payload = ExtractArguments(raw);
                            payload["action"] = "folder";
                            if (!payload.HasKey("path") && payload.HasKey("folderPath"))
                            {
                                payload["path"] = payload["folderPath"];
                            }
                            return payload;
                        }),
                        ["asset_get_info"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                try
                                {
                                    var payload = ExtractArguments(arguments);
                                    string path = payload.GetString("path");
                                    if (string.IsNullOrEmpty(path))
                                    {
                                        sendError("Missing 'path' for asset_get_info.");
                                        return;
                                    }
        
                                    var result = new JSONObject();
                                    result["path"] = path;
                                    result["exists"] = AssetDatabase.LoadMainAssetAtPath(path) != null;
                                    result["guid"] = AssetDatabase.AssetPathToGUID(path);
                                    result["type"] = AssetDatabase.GetMainAssetTypeAtPath(path)?.FullName ?? "Unknown";
                                    SendResult(sendResponse, result);
                                }
                                catch (Exception e)
                                {
                                    sendError($"asset_get_info failed: {e.Message}");
                                }
                            });
                        }
                    };
        
                    return new UnityModuleTool(
                        "unity_asset",
                        "Asset import, move, query, and refresh workflows.",
                        docs,
                        handlers,
                        "Advanced file IO and import settings can be done through unity_asset_meta and unity_asset_manage.");
                }
    }
}
