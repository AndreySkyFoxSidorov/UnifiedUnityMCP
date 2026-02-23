using System;
using System.Text;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class AssetMetaManageTool : ITool
    {
        public string Name => "unity.asset.meta";
        public string Description => "Read or write properties inside an asset's .meta file (Importer settings) using SerializedProperty paths.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'get' or 'set' or 'dump'.");
                props["path"] = McpMessages.CreateStringProperty("Asset path (e.g. Assets/Textures/Icon.png).");
                props["property"] = McpMessages.CreateStringProperty("SerializedProperty path (e.g. 'm_TextureSettings.m_FilterMode'). Used for get/set.");
                props["value"] = new JSONObject();
                props["value"]["description"] = "Value to set (only for 'set' action). Can be string, number, or bool.";

                var required = new JSONArray();
                required.Add("action");
                required.Add("path");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();
            string path = arguments.GetString("path");
            string propName = arguments.GetString("property");
            JSONNode valueNode = arguments["value"];

            if (string.IsNullOrEmpty(path))
            {
                sendError("Missing 'path'.");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    AssetImporter importer = AssetImporter.GetAtPath(path);
                    if (importer == null)
                    {
                        sendError($"Could not load AssetImporter for path '{path}'. Is it a valid asset?");
                        return;
                    }

                    var so = new SerializedObject(importer);

                    if (action == "dump")
                    {
                        var arr = new JSONArray();
                        var prop = so.GetIterator();
                        bool enterChildren = true;
                        while (prop.NextVisible(enterChildren))
                        {
                            var obj = new JSONObject();
                            obj["path"] = prop.propertyPath;
                            obj["type"] = prop.propertyType.ToString();
                            obj["value"] = GetPropertyValueAsString(prop);
                            arr.Add(obj);
                            enterChildren = false; // NextVisible handles hierarchy, but we can do a flat walk or tree walk. Let's do flat for dump.
                            if (prop.hasVisibleChildren) enterChildren = true;
                        }

                        var res = new JSONObject();
                        res["properties"] = arr;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    if (string.IsNullOrEmpty(propName))
                    {
                        sendError("Missing 'property' name for get/set.");
                        return;
                    }

                    var targetProp = so.FindProperty(propName);
                    if (targetProp == null)
                    {
                        sendError($"Property '{propName}' not found in importer for '{path}'. Use 'dump' to see available properties.");
                        return;
                    }

                    if (action == "get")
                    {
                        var res = new JSONObject();
                        res["property"] = propName;
                        res["type"] = targetProp.propertyType.ToString();
                        res["value"] = GetPropertyValueAsString(targetProp);
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

                        if (!SetPropertyValue(targetProp, valueNode, out string error))
                        {
                            sendError($"Failed to set property '{propName}': {error}");
                            return;
                        }

                        so.ApplyModifiedProperties();
                        importer.SaveAndReimport();

                        var res = new JSONObject();
                        res["status"] = "Success";
                        res["property"] = propName;
                        res["newValue"] = GetPropertyValueAsString(targetProp);
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    sendError($"Invalid action '{action}'. Use 'get', 'set', or 'dump'.");
                }
                catch (Exception e)
                {
                    sendError($"AssetMetaManageTool failed: {e.Message}");
                }
            });
        }

        private string GetPropertyValueAsString(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer: return prop.intValue.ToString();
                case SerializedPropertyType.Boolean: return prop.boolValue.ToString();
                case SerializedPropertyType.Float: return prop.floatValue.ToString();
                case SerializedPropertyType.String: return prop.stringValue;
                case SerializedPropertyType.Enum: return prop.enumNames.Length > prop.enumValueIndex && prop.enumValueIndex >= 0 ? prop.enumNames[prop.enumValueIndex] : prop.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference: return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "null";
                default: return $"[{prop.propertyType}]";
            }
        }

        private bool SetPropertyValue(SerializedProperty prop, JSONNode node, out string error)
        {
            error = null;
            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        prop.intValue = node.AsInt;
                        return true;
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = node.AsBool;
                        return true;
                    case SerializedPropertyType.Float:
                        prop.floatValue = node.AsFloat;
                        return true;
                    case SerializedPropertyType.String:
                        prop.stringValue = node.Value;
                        return true;
                    case SerializedPropertyType.Enum:
                        // Try string map first
                        string strVal = node.Value;
                        int idx = Array.IndexOf(prop.enumNames, strVal);
                        if (idx >= 0)
                        {
                            prop.enumValueIndex = idx;
                            return true;
                        }
                        // Fallback to integer
                        if (int.TryParse(strVal, out int intVal))
                        {
                            prop.enumValueIndex = intVal;
                            return true;
                        }
                        error = $"Invalid enum value '{strVal}'. Valid names: {string.Join(", ", prop.enumNames)}";
                        return false;
                    default:
                        error = $"Setting properties of type {prop.propertyType} is not supported directly. Needs raw file parse.";
                        return false;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}
