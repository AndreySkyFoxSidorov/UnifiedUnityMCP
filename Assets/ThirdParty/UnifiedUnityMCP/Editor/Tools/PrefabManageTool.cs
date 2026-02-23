using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Mcp.Editor.Tools
{
    public class PrefabManageTool : ITool
    {
        public string Name => "unity_prefab_instantiate";
        public string Description => "Instantiate a prefab from an asset path into the active scene.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["assetPath"] = McpMessages.CreateStringProperty("Path to the prefab (e.g. 'Assets/Prefabs/MyPrefab.prefab').");

                var required = new JSONArray();
                required.Add("assetPath");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string assetPath = arguments.GetString("assetPath");
            if (string.IsNullOrEmpty(assetPath))
            {
                sendError("Missing parameter: 'assetPath'");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab == null)
                    {
                        sendError($"Prefab not found at '{assetPath}'.");
                        return;
                    }

                    var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (instance == null)
                    {
                        sendError($"Failed to instantiate prefab from '{assetPath}'.");
                        return;
                    }

                    Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {prefab.name}");
                    Selection.activeGameObject = instance;

                    var res = new JSONObject();
                    res["status"] = "Instantiated";
                    res["instanceId"] = instance.GetInstanceID();
                    res["name"] = instance.name;
                    sendResponse(McpMessages.CreateToolResult(res.ToString()));
                }
                catch (Exception e)
                {
                    sendError($"Prefab integration failed: {e.Message}");
                }
            });
        }
    }
}
