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
        private static UnityModuleTool CreateBookmarkModule()
                {
                    var docs = CreateDocs("bookmark_set", "bookmark_goto", "bookmark_list", "bookmark_delete", "bridge");
                    var handlers = new Dictionary<string, ModuleActionHandler>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["bridge"] = CreateBridgeHandler("unity_bookmark"),
                        ["bookmark_set"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var payload = ExtractArguments(arguments);
                                string name = payload.GetString("name");
                                if (string.IsNullOrEmpty(name))
                                {
                                    sendError("Missing 'name' for bookmark_set.");
                                    return;
                                }
        
                                var view = SceneView.lastActiveSceneView;
                                if (view == null)
                                {
                                    sendError("No active SceneView found.");
                                    return;
                                }
        
                                _bookmarks[name] = new SceneBookmark
                                {
                                    Pivot = view.pivot,
                                    Rotation = view.rotation,
                                    Size = view.size,
                                    Orthographic = view.orthographic
                                };
        
                                var result = new JSONObject();
                                result["status"] = "saved";
                                result["name"] = name;
                                SendResult(sendResponse, result);
                            });
                        },
                        ["bookmark_goto"] = (arguments, sendResponse, sendError) =>
                        {
                            MainThreadDispatcher.InvokeAsync(() =>
                            {
                                var payload = ExtractArguments(arguments);
                                string name = payload.GetString("name");
                                if (string.IsNullOrEmpty(name))
                                {
                                    sendError("Missing 'name' for bookmark_goto.");
                                    return;
                                }
        
                                if (!_bookmarks.TryGetValue(name, out var bookmark))
                                {
                                    sendError($"Bookmark not found: {name}");
                                    return;
                                }
        
                                var view = SceneView.lastActiveSceneView;
                                if (view == null)
                                {
                                    sendError("No active SceneView found.");
                                    return;
                                }
        
                                view.pivot = bookmark.Pivot;
                                view.rotation = bookmark.Rotation;
                                view.size = bookmark.Size;
                                view.orthographic = bookmark.Orthographic;
                                view.Repaint();
        
                                var result = new JSONObject();
                                result["status"] = "moved";
                                result["name"] = name;
                                SendResult(sendResponse, result);
                            });
                        },
                        ["bookmark_list"] = (arguments, sendResponse, sendError) =>
                        {
                            var result = new JSONObject();
                            var arr = new JSONArray();
                            foreach (var key in _bookmarks.Keys.OrderBy(k => k))
                            {
                                arr.Add(key);
                            }
                            result["count"] = _bookmarks.Count;
                            result["bookmarks"] = arr;
                            SendResult(sendResponse, result);
                        },
                        ["bookmark_delete"] = (arguments, sendResponse, sendError) =>
                        {
                            var payload = ExtractArguments(arguments);
                            string name = payload.GetString("name");
                            if (string.IsNullOrEmpty(name))
                            {
                                sendError("Missing 'name' for bookmark_delete.");
                                return;
                            }
        
                            bool removed = _bookmarks.Remove(name);
                            var result = new JSONObject();
                            result["status"] = removed ? "deleted" : "not_found";
                            result["name"] = name;
                            SendResult(sendResponse, result);
                        }
                    };
        
                    return new UnityModuleTool("unity_bookmark", "Scene view bookmarks.", docs, handlers, "Bookmarks are stored in-memory for the current editor session.");
                }
    }
}
