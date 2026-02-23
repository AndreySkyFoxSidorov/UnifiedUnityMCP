using System;
using System.Reflection;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Mcp.Editor.Tools
{
    public class ComponentPropertyTool : ITool
    {
        public string Name => "unity.component.property";
        public string Description => "Read or write fields/properties on a Component using reflection. Actions: 'get', 'set'. Primitive types and Vector2/3 only.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'get' or 'set'.");
                props["instanceId"] = McpMessages.CreateIntegerProperty("InstanceID of the Component (get from unity.component.manage).");
                props["property"] = McpMessages.CreateStringProperty("Name of the field or property.");
                props["value"] = new JSONObject();
                props["value"]["description"] = "Value to set (only for 'set' action). Can be string, number, bool, or object for vectors.";

                var required = new JSONArray();
                required.Add("action");
                required.Add("instanceId");
                required.Add("property");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();
            int instanceId = arguments.GetInt("instanceId", 0);
            string propName = arguments.GetString("property");
            JSONNode valueNode = arguments["value"];

            if (instanceId == 0 || string.IsNullOrEmpty(propName))
            {
                sendError("Missing 'instanceId' or 'property'.");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
#pragma warning disable CS0618
                    var comp = EditorUtility.InstanceIDToObject(instanceId) as Component;
#pragma warning restore CS0618
                    if (comp == null)
                    {
                        sendError($"Component with ID {instanceId} not found.");
                        return;
                    }

                    Type type = comp.GetType();
                    PropertyInfo propInfo = type.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    FieldInfo fieldInfo = type.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (propInfo == null && fieldInfo == null)
                    {
                        sendError($"No field or property named '{propName}' found on '{type.Name}'.");
                        return;
                    }

                    Type memberType = propInfo != null ? propInfo.PropertyType : fieldInfo.FieldType;

                    if (action == "get")
                    {
                        object val = propInfo != null ? propInfo.GetValue(comp) : fieldInfo.GetValue(comp);
                        var res = new JSONObject();
                        res["property"] = propName;
                        if (val == null) res["value"] = "null";
                        else if (val is Vector3 v3) res["value"] = new JSONObject { ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z };
                        else if (val is Vector2 v2) res["value"] = new JSONObject { ["x"] = v2.x, ["y"] = v2.y };
                        else if (val is Color c) res["value"] = new JSONObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
                        else res["value"] = val.ToString();
                        
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    if (action == "set")
                    {
                        if (valueNode == null)
                        {
                            sendError("Missing 'value' for 'set' action.");
                            return;
                        }

                        Undo.RecordObject(comp, $"Set {propName}");
                        object finalValue = ParseValue(valueNode, memberType);
                        
                        if (propInfo != null)
                        {
                            if (!propInfo.CanWrite)
                            {
                                sendError($"Property '{propName}' is read-only.");
                                return;
                            }
                            propInfo.SetValue(comp, finalValue);
                        }
                        else
                        {
                            fieldInfo.SetValue(comp, finalValue);
                        }

                        EditorUtility.SetDirty(comp);

                        var res = new JSONObject();
                        res["status"] = "Success";
                        res["property"] = propName;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    sendError($"Invalid action '{action}'. Use 'get' or 'set'.");
                }
                catch (Exception e)
                {
                    sendError($"Component Property Tool failed: {e.Message}");
                }
            });
        }

        private object ParseValue(JSONNode node, Type targetType)
        {
            if (targetType == typeof(int)) return node.AsInt;
            if (targetType == typeof(float)) return node.AsFloat;
            if (targetType == typeof(double)) return node.AsDouble;
            if (targetType == typeof(bool)) return node.AsBool;
            if (targetType == typeof(string)) return node.Value;
            
            if (targetType == typeof(Vector3) && node.IsObject)
            {
                var obj = node.AsObject;
                return new Vector3(obj["x"].AsFloat, obj["y"].AsFloat, obj["z"].AsFloat);
            }
            if (targetType == typeof(Vector2) && node.IsObject)
            {
                var obj = node.AsObject;
                return new Vector2(obj["x"].AsFloat, obj["y"].AsFloat);
            }
            if (targetType == typeof(Color) && node.IsObject)
            {
                var obj = node.AsObject;
                return new Color(obj["r"].AsFloat, obj["g"].AsFloat, obj["b"].AsFloat, obj["a"].AsFloat);
            }
            
            // Fallback for enums
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, node.Value, true);
            }

            return Convert.ChangeType(node.Value, targetType);
        }
    }
}
