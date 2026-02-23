using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Mcp.Editor.Tools
{
    public class ComponentManageTool : ITool
    {
        public string Name => "unity_component_manage";
        public string Description => "Manage components on GameObjects. Actions: 'add', 'remove', 'list'.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'add', 'remove', 'list'");
                props["instanceId"] = McpMessages.CreateIntegerProperty("Instance ID of the GameObject.");
                props["componentType"] = McpMessages.CreateStringProperty("Type of the component (e.g. 'UnityEngine.Rigidbody').");

                var required = new JSONArray();
                required.Add("action");
                required.Add("instanceId");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    string action = arguments.GetString("action")?.ToLower();
                    int instanceId = arguments.GetInt("instanceId", 0);
                    string compTypeStr = arguments.GetString("componentType");

                    if (instanceId == 0)
                    {
                        sendError("Valid 'instanceId' is required.");
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

                    if (action == "list")
                    {
                        var arr = new JSONArray();
                        foreach (var c in go.GetComponents<Component>())
                        {
                            if (c != null)
                            {
                                var cobj = new JSONObject();
                                cobj["type"] = c.GetType().FullName;
                                cobj["instanceId"] = c.GetInstanceID();
                                arr.Add(cobj);
                            }
                        }
                        var res = new JSONObject();
                        res["components"] = arr;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    if (string.IsNullOrEmpty(compTypeStr))
                    {
                        sendError("'componentType' is required for add/remove actions.");
                        return;
                    }

                    // Get type via standard or Unity assemblies
                    Type type = GetTypeByName(compTypeStr);
                    if (type == null)
                    {
                        sendError($"Component type '{compTypeStr}' could not be resolved.");
                        return;
                    }

                    if (!typeof(Component).IsAssignableFrom(type))
                    {
                        sendError($"Type '{type.FullName}' is not a UnityEngine.Component.");
                        return;
                    }

                    if (action == "add")
                    {
                        var newComp = Undo.AddComponent(go, type);
                        var res = new JSONObject();
                        res["status"] = "Added";
                        res["componentInstanceId"] = newComp.GetInstanceID();
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                    }
                    else if (action == "remove")
                    {
                        var comp = go.GetComponent(type);
                        if (comp != null)
                        {
                            Undo.DestroyObjectImmediate(comp);
                            var res = new JSONObject();
                            res["status"] = "Removed";
                            sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        }
                        else
                        {
                            sendError($"Component of type '{type.FullName}' not found on object '{go.name}'.");
                        }
                    }
                    else
                    {
                        sendError($"Invalid action '{action}'.");
                    }
                }
                catch (Exception e)
                {
                    sendError($"Component manage failed: {e.Message}");
                }
            });
        }

        private Type GetTypeByName(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType(name);
                if (t != null) return t;
            }
            // Try prefix if they just passed "Rigidbody"
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType("UnityEngine." + name);
                if (t != null) return t;
            }
            return null;
        }
    }
}
