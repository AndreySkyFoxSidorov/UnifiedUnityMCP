using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Mcp.Editor.Tools
{
    public class SceneManageTool : ITool
    {
        public string Name => "unity.scene.manage";
        public string Description => "Manage levels/scenes. Actions: 'open', 'save', 'new', 'list_build_scenes'.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'open', 'save', 'new', or 'list_build_scenes'.");
                props["path"] = McpMessages.CreateStringProperty("Asset path for 'open' (e.g. Assets/Scenes/Main.unity).");

                var required = new JSONArray();
                required.Add("action");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();
            string path = arguments.GetString("path");

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (action == "list_build_scenes")
                    {
                        var arr = new JSONArray();
                        foreach (var scene in EditorBuildSettings.scenes)
                        {
                            var obj = new JSONObject();
                            obj["path"] = scene.path;
                            obj["enabled"] = scene.enabled;
                            arr.Add(obj);
                        }
                        var res = new JSONObject();
                        res["buildScenes"] = arr;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    if (action == "save")
                    {
                        bool success = EditorSceneManager.SaveOpenScenes();
                        sendResponse(McpMessages.CreateToolResult($"{{\"status\":\"{(success ? "Saved" : "Failed")}\"}}"));
                        return;
                    }

                    if (action == "new")
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                            sendResponse(McpMessages.CreateToolResult($"{{\"status\":\"New scene created\", \"name\":\"{scene.name}\"}}"));
                        }
                        else
                        {
                            sendError("User cancelled save prompt.");
                        }
                        return;
                    }

                    if (action == "open")
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            sendError("Missing 'path' for open action.");
                            return;
                        }

                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                            sendResponse(McpMessages.CreateToolResult($"{{\"status\":\"Opened\", \"path\":\"{scene.path}\", \"isValid\":{scene.IsValid().ToString().ToLower()}}}"));
                        }
                        else
                        {
                            sendError("User cancelled save prompt.");
                        }
                        return;
                    }

                    sendError($"Invalid action '{action}'.");
                }
                catch (Exception e)
                {
                    sendError($"Scene tool failed: {e.Message}");
                }
            });
        }
    }
}
