using System;
using System.Collections.Generic;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mcp.Editor.Tools
{
    public class GameObjectManageTool : ITool
    {
        public string Name => "unity_gameobject_manage";
        public string Description => "CRUD operations for GameObjects in the scene. Actions: 'find', 'create', 'destroy'.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action to perform: 'find', 'create', or 'destroy'.");
                props["name"] = McpMessages.CreateStringProperty("Name of the object to find/create.");
                props["tag"] = McpMessages.CreateStringProperty("Tag of the object to find.");
                props["instanceId"] = McpMessages.CreateIntegerProperty("Instance ID of the object to destroy.");
                props["primitiveType"] = McpMessages.CreateStringProperty("Use for create: 'Cube', 'Sphere', 'Capsule', 'Cylinder', 'Plane', 'Quad'.");

                var required = new JSONArray();
                required.Add("action");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();
            if (string.IsNullOrEmpty(action))
            {
                sendError("Missing required parameter: 'action'");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    switch (action)
                    {
                        case "find":
                            HandleFind(arguments, sendResponse);
                            break;
                        case "create":
                            HandleCreate(arguments, sendResponse);
                            break;
                        case "destroy":
                            HandleDestroy(arguments, sendResponse, sendError);
                            break;
                        default:
                            sendError($"Invalid action '{action}'. Use 'find', 'create', 'destroy'.");
                            break;
                    }
                }
                catch (Exception e)
                {
                    sendError($"GameObject manage failed: {e.Message}");
                }
            });
        }

        private void HandleFind(JSONObject args, Action<JSONObject> sendResponse)
        {
            string name = args.GetString("name");
            string tag = args.GetString("tag");

            var results = new JSONArray();
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (var go in allObjects)
            {
                // Only consider scene objects, not prefabs
                if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                    continue;

                if (!string.IsNullOrEmpty(go.scene.name))
                {
                    bool matchName = string.IsNullOrEmpty(name) || go.name == name;
                    bool matchTag = string.IsNullOrEmpty(tag) || go.CompareTag(tag);

                    if (matchName && matchTag)
                    {
                        var info = new JSONObject();
                        info["name"] = go.name;
                        info["instanceId"] = go.GetInstanceID();
                        info["scene"] = go.scene.name;
                        results.Add(info);
                    }
                }
            }

            var resultObj = new JSONObject();
            resultObj["found"] = results;
            sendResponse(McpMessages.CreateToolResult(resultObj.ToString()));
        }

        private void HandleCreate(JSONObject args, Action<JSONObject> sendResponse)
        {
            string name = args.GetString("name", "New GameObject");
            string primStr = args.GetString("primitiveType");

            GameObject go;
            if (!string.IsNullOrEmpty(primStr) && Enum.TryParse(primStr, true, out PrimitiveType pType))
            {
                go = GameObject.CreatePrimitive(pType);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            var resultObj = new JSONObject();
            resultObj["status"] = "Created";
            resultObj["name"] = go.name;
            resultObj["instanceId"] = go.GetInstanceID();
            sendResponse(McpMessages.CreateToolResult(resultObj.ToString()));
        }

        private void HandleDestroy(JSONObject args, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            int instanceId = args.GetInt("instanceId", 0);
            if (instanceId == 0)
            {
                sendError("Missing or invalid 'instanceId' for destroy action.");
                return;
            }

#pragma warning disable CS0618
            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#pragma warning restore CS0618
            if (go == null)
            {
                sendError($"GameObject with instance ID {instanceId} not found.");
                return;
            }

            Undo.DestroyObjectImmediate(go);

            var resultObj = new JSONObject();
            resultObj["status"] = "Destroyed";
            resultObj["instanceId"] = instanceId;
            sendResponse(McpMessages.CreateToolResult(resultObj.ToString()));
        }
    }
}
