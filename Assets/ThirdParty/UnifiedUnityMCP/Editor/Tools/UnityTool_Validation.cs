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
        private static UnityModuleTool CreateValidationModule()
                {
                    var docs = CreateDocs(
                        "validate_scene",
                        "validate_find_missing_scripts",
                        "validate_fix_missing_scripts",
                        "validate_cleanup_empty_folders",
                        "validate_find_unused_assets",
                        "validate_texture_sizes",
                        "validate_project_structure",
                        "bridge");
        
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_validation"),
                        ["validate_scene"] = Forward("unity_validation", raw =>
                        {
                            var payload = new JSONObject();
                            payload["action"] = "validate_find_missing_scripts";
                            return payload;
                        }),
                        ["validate_find_missing_scripts"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var issues = new JSONArray();
                                int totalMissing = 0;
        
                                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                                foreach (var go in all)
                                {
                                    if (go == null || string.IsNullOrEmpty(go.scene.name))
                                    {
                                        continue;
                                    }
        
                                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                                    if (missing <= 0)
                                    {
                                        continue;
                                    }
        
                                    totalMissing += missing;
                                    var entry = new JSONObject();
                                    entry["name"] = go.name;
                                    entry["path"] = GetGameObjectPath(go.transform);
                                    entry["missingCount"] = missing;
                                    issues.Add(entry);
                                }
        
                                var result = new JSONObject();
                                result["count"] = issues.Count;
                                result["missingScripts"] = totalMissing;
                                result["objectsWithMissingScripts"] = issues;
                                SendResult(sendResponse, result);
                            });
                        },
                        ["validate_fix_missing_scripts"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var payload = ExtractArguments(arguments);
                                bool dryRun = payload.GetBool("dryRun", true);
        
                                int total = 0;
                                var fixedObjects = new JSONArray();
        
                                var all = Resources.FindObjectsOfTypeAll<GameObject>();
                                foreach (var go in all)
                                {
                                    if (go == null || string.IsNullOrEmpty(go.scene.name))
                                    {
                                        continue;
                                    }
        
                                    int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                                    if (missing <= 0)
                                    {
                                        continue;
                                    }
        
                                    int fixedCount = missing;
                                    if (!dryRun)
                                    {
                                        fixedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                                    }
        
                                    total += fixedCount;
                                    var entry = new JSONObject();
                                    entry["name"] = go.name;
                                    entry["fixed"] = fixedCount;
                                    fixedObjects.Add(entry);
                                }
        
                                var result = new JSONObject();
                                result["dryRun"] = dryRun;
                                result["totalFixed"] = total;
                                result["fixedObjects"] = fixedObjects;
                                SendResult(sendResponse, result);
                            });
                        },
                        ["validate_texture_sizes"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var payload = ExtractArguments(arguments);
                                int maxSize = payload.GetInt("maxRecommendedSize", 2048);
                                int limit = payload.GetInt("limit", 50);
        
                                var guids = AssetDatabase.FindAssets("t:Texture2D");
                                var oversized = new JSONArray();
                                int checkedCount = 0;
        
                                foreach (var guid in guids)
                                {
                                    string path = AssetDatabase.GUIDToAssetPath(guid);
                                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                                    if (tex == null)
                                    {
                                        continue;
                                    }
        
                                    checkedCount++;
                                    if (tex.width <= maxSize && tex.height <= maxSize)
                                    {
                                        continue;
                                    }
        
                                    var item = new JSONObject();
                                    item["path"] = path;
                                    item["width"] = tex.width;
                                    item["height"] = tex.height;
                                    oversized.Add(item);
        
                                    if (oversized.Count >= limit)
                                    {
                                        break;
                                    }
                                }
        
                                var result = new JSONObject();
                                result["totalChecked"] = checkedCount;
                                result["oversizedCount"] = oversized.Count;
                                result["oversizedTextures"] = oversized;
                                SendResult(sendResponse, result);
                            });
                        },
                        ["validate_project_structure"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var payload = ExtractArguments(arguments);
                                string rootPath = payload.GetString("rootPath", "Assets");
                                int maxDepth = payload.GetInt("maxDepth", 3);
        
                                string fullRoot = ResolveProjectPath(rootPath);
                                if (!Directory.Exists(fullRoot))
                                {
                                    sendError($"Path does not exist: {rootPath}");
                                    return;
                                }
        
                                var structure = BuildDirectoryTree(fullRoot, rootPath.Replace("\\", "/"), 0, maxDepth);
                                var result = new JSONObject();
                                result["rootPath"] = rootPath;
                                result["structure"] = structure;
                                SendResult(sendResponse, result);
                            });
                        }
                    };
        
                    return new UnityModuleTool("unity_validation", "Project validation and cleanup module.", docs, handlers, "Unused-asset and destructive cleanup flows should be reviewed manually before applying deletes.");
                }
    }
}
