using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Linq;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Mcp.Editor.Tools
{
    public class ComponentPropertyTool : ITool
    {
        public string Name => "unity_component_property";
        public string Description => "Read/write/inspect/invoke component members using reflection. Actions: 'get', 'set', 'dump', 'invoke'.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'get', 'set', 'dump', or 'invoke'.");
                props["instanceId"] = McpMessages.CreateIntegerProperty("InstanceID of a Component or GameObject. If GameObject is provided, optional 'componentType' selects component.");
                props["property"] = McpMessages.CreateStringProperty("Name of field/property (for get/set) or method name (for invoke).");
                props["value"] = new JSONObject();
                props["value"]["description"] = "Value to set (only for 'set' action). Can be string, number, bool, or object for vectors.";
                props["componentType"] = McpMessages.CreateStringProperty("Optional exact component type name when instanceId belongs to a GameObject.");
                props["args"] = new JSONObject();
                props["args"]["type"] = "array";
                props["args"]["description"] = "Optional argument array for action='invoke'.";

                var required = new JSONArray();
                required.Add("action");
                required.Add("instanceId");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();
            int instanceId = arguments.GetInt("instanceId", 0);
            string propName = arguments.GetString("property");
            JSONNode valueNode = arguments["value"];
            JSONArray invokeArgs = arguments.GetArray("args");
            string componentType = arguments.GetString("componentType");

            if (instanceId == 0)
            {
                sendError("Missing 'instanceId'.");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
#pragma warning disable CS0618
                    var sourceObject = EditorUtility.InstanceIDToObject(instanceId);
#pragma warning restore CS0618
                    Component comp = ResolveComponent(sourceObject, componentType);

                    if (comp == null)
                    {
                        sendError($"Component for ID {instanceId} not found.");
                        return;
                    }

                    Type type = comp.GetType();

                    if (action == "invoke")
                    {
                        if (string.IsNullOrEmpty(propName))
                        {
                            sendError("Missing 'property'. This field is required for 'invoke' action and must contain method name.");
                            return;
                        }

                        if (!TryInvokeMethod(comp, type, propName, invokeArgs, sendResponse, sendError))
                        {
                            return;
                        }

                        return;
                    }

                    if (action == "dump")
                    {
                        var dump = new JSONArray();

                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var field in fields)
                        {
                            if (field.IsSpecialName)
                            {
                                continue;
                            }

                            if (field.Name.Contains("k__BackingField"))
                            {
                                continue;
                            }

                            object value = null;
                            try
                            {
                                value = field.GetValue(comp);
                            }
                            catch
                            {
                                // Ignore unreadable field
                            }

                            var info = new JSONObject();
                            info["name"] = field.Name;
                            info["kind"] = "field";
                            info["type"] = field.FieldType.FullName;
                            info["canWrite"] = !field.IsInitOnly;
                            info["value"] = SerializeValue(value);
                            dump.Add(info);
                        }

                        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var prop in properties)
                        {
                            if (prop.GetIndexParameters().Length > 0)
                            {
                                continue;
                            }

                            object value = null;
                            bool canRead = prop.CanRead;
                            if (canRead)
                            {
                                try
                                {
                                    value = prop.GetValue(comp);
                                }
                                catch
                                {
                                    // Ignore unreadable property getter exceptions
                                }
                            }

                            var info = new JSONObject();
                            info["name"] = prop.Name;
                            info["kind"] = "property";
                            info["type"] = prop.PropertyType.FullName;
                            info["canWrite"] = prop.CanWrite;
                            info["value"] = SerializeValue(value);
                            dump.Add(info);
                        }

                        var res = new JSONObject();
                        res["members"] = dump;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                        return;
                    }

                    if (string.IsNullOrEmpty(propName))
                    {
                        sendError("Missing 'property'. This field is required for 'get' and 'set' actions.");
                        return;
                    }

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
                        res["value"] = SerializeValue(val);

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

                    sendError($"Invalid action '{action}'. Use 'get', 'set', 'dump', or 'invoke'.");
                }
                catch (Exception e)
                {
                    sendError($"Component Property Tool failed: {e.Message}");
                }
            });
        }

        private Component ResolveComponent(UnityEngine.Object sourceObject, string componentType)
        {
            if (sourceObject is Component readyComponent)
            {
                if (string.IsNullOrEmpty(componentType))
                {
                    return readyComponent;
                }

                if (string.Equals(readyComponent.GetType().FullName, componentType, StringComparison.Ordinal) ||
                    string.Equals(readyComponent.GetType().Name, componentType, StringComparison.Ordinal))
                {
                    return readyComponent;
                }

                var host = readyComponent.gameObject;
                return FindComponentByTypeName(host, componentType);
            }

            if (sourceObject is GameObject go)
            {
                if (string.IsNullOrEmpty(componentType))
                {
                    return go.transform;
                }

                return FindComponentByTypeName(go, componentType);
            }

            return null;
        }

        private Component FindComponentByTypeName(GameObject go, string componentType)
        {
            if (go == null || string.IsNullOrEmpty(componentType))
            {
                return null;
            }

            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null)
                {
                    continue;
                }

                var t = comp.GetType();
                if (string.Equals(t.FullName, componentType, StringComparison.Ordinal) ||
                    string.Equals(t.Name, componentType, StringComparison.Ordinal))
                {
                    return comp;
                }
            }

            return null;
        }

        private bool TryInvokeMethod(Component comp, Type type, string methodName, JSONArray argsNode, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
                .ToArray();

            if (methods.Length == 0)
            {
                sendError($"Method '{methodName}' not found on '{type.Name}'.");
                return false;
            }

            var callArgs = argsNode != null ? argsNode : new JSONArray();

            MethodInfo target = null;
            object[] finalArgs = null;

            foreach (var candidate in methods)
            {
                var parameters = candidate.GetParameters();
                if (parameters.Length != callArgs.Count)
                {
                    continue;
                }

                var parsedArgs = new object[parameters.Length];
                bool parsed = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    try
                    {
                        parsedArgs[i] = ParseValue(callArgs[i], parameters[i].ParameterType);
                    }
                    catch
                    {
                        parsed = false;
                        break;
                    }
                }

                if (parsed)
                {
                    target = candidate;
                    finalArgs = parsedArgs;
                    break;
                }
            }

            if (target == null)
            {
                sendError($"No compatible overload found for method '{methodName}' with {callArgs.Count} argument(s).");
                return false;
            }

            Undo.RecordObject(comp, $"Invoke {methodName}");
            object returnValue = target.Invoke(comp, finalArgs);
            EditorUtility.SetDirty(comp);

            var result = new JSONObject();
            result["status"] = "Success";
            result["method"] = methodName;
            result["returnValue"] = SerializeValue(returnValue);
            sendResponse(McpMessages.CreateToolResult(result.ToString()));
            return true;
        }

        private object ParseValue(JSONNode node, Type targetType)
        {
            if (targetType == typeof(int)) return node.AsInt;
            if (targetType == typeof(float)) return node.AsFloat;
            if (targetType == typeof(double)) return node.AsDouble;
            if (targetType == typeof(bool)) return node.AsBool;
            if (targetType == typeof(string)) return node.Value;
            if (targetType == typeof(long)) return ParseLong(node);
            if (targetType == typeof(short)) return Convert.ToInt16(node.AsInt);
            if (targetType == typeof(byte)) return Convert.ToByte(node.AsInt);
            if (targetType == typeof(uint)) return Convert.ToUInt32(node.Value, CultureInfo.InvariantCulture);
            if (targetType == typeof(ulong)) return Convert.ToUInt64(node.Value, CultureInfo.InvariantCulture);
            if (targetType == typeof(decimal)) return Convert.ToDecimal(node.Value, CultureInfo.InvariantCulture);
            if (targetType == typeof(char)) return ParseChar(node);

            if (targetType == typeof(Vector3Int) && node.IsObject)
            {
                var obj = node.AsObject;
                return new Vector3Int(obj["x"].AsInt, obj["y"].AsInt, obj["z"].AsInt);
            }
            if (targetType == typeof(Vector2Int) && node.IsObject)
            {
                var obj = node.AsObject;
                return new Vector2Int(obj["x"].AsInt, obj["y"].AsInt);
            }
            if (targetType == typeof(Quaternion) && node.IsObject)
            {
                var obj = node.AsObject;
                if (obj.HasKey("x") && obj.HasKey("y") && obj.HasKey("z") && obj.HasKey("w"))
                {
                    return new Quaternion(obj["x"].AsFloat, obj["y"].AsFloat, obj["z"].AsFloat, obj["w"].AsFloat);
                }

                return Quaternion.Euler(obj["x"].AsFloat, obj["y"].AsFloat, obj["z"].AsFloat);
            }

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

            if (targetType == typeof(Color32) && node.IsObject)
            {
                var obj = node.AsObject;
                return new Color32((byte)obj["r"].AsInt, (byte)obj["g"].AsInt, (byte)obj["b"].AsInt, (byte)obj["a"].AsInt);
            }

            if (targetType == typeof(Rect) && node.IsObject)
            {
                var obj = node.AsObject;
                return new Rect(obj["x"].AsFloat, obj["y"].AsFloat, obj["width"].AsFloat, obj["height"].AsFloat);
            }

            if (targetType == typeof(Bounds) && node.IsObject)
            {
                var obj = node.AsObject;
                var center = obj["center"].AsObject;
                var size = obj["size"].AsObject;
                return new Bounds(
                    new Vector3(center["x"].AsFloat, center["y"].AsFloat, center["z"].AsFloat),
                    new Vector3(size["x"].AsFloat, size["y"].AsFloat, size["z"].AsFloat));
            }

            if (targetType == typeof(LayerMask))
            {
                return new LayerMask { value = node.AsInt };
            }

            if (targetType == typeof(UnityEngine.Object))
            {
                return ParseUnityObject(node, targetType);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
            {
                return ParseUnityObject(node, targetType);
            }

            if (targetType.IsArray && node.IsArray)
            {
                var arrayNode = node.AsArray;
                Type elementType = targetType.GetElementType();
                Array arr = Array.CreateInstance(elementType, arrayNode.Count);
                for (int i = 0; i < arrayNode.Count; i++)
                {
                    arr.SetValue(ParseValue(arrayNode[i], elementType), i);
                }
                return arr;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
            {
                Type elementType = targetType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(targetType);
                if (node.IsArray)
                {
                    foreach (var item in node.AsArray)
                    {
                        list.Add(ParseValue(item, elementType));
                    }
                }
                return list;
            }

            // Fallback for enums
            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, node.Value, true);
            }

            return Convert.ChangeType(node.Value, targetType);
        }

        private JSONNode SerializeValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is int i) return i;
            if (value is long l) return l.ToString(CultureInfo.InvariantCulture);
            if (value is float f) return f;
            if (value is double d) return d;
            if (value is decimal m) return m.ToString(CultureInfo.InvariantCulture);
            if (value is bool b) return b;
            if (value is string s) return s;
            if (value is char ch) return ch.ToString();

            if (value is Vector2 v2)
            {
                return new JSONObject { ["x"] = v2.x, ["y"] = v2.y };
            }

            if (value is Vector3 v3)
            {
                return new JSONObject { ["x"] = v3.x, ["y"] = v3.y, ["z"] = v3.z };
            }

            if (value is Vector2Int v2i)
            {
                return new JSONObject { ["x"] = v2i.x, ["y"] = v2i.y };
            }

            if (value is Vector3Int v3i)
            {
                return new JSONObject { ["x"] = v3i.x, ["y"] = v3i.y, ["z"] = v3i.z };
            }

            if (value is Quaternion q)
            {
                return new JSONObject { ["x"] = q.x, ["y"] = q.y, ["z"] = q.z, ["w"] = q.w };
            }

            if (value is Color c)
            {
                return new JSONObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
            }

            if (value is Color32 c32)
            {
                return new JSONObject
                {
                    ["r"] = (int)c32.r,
                    ["g"] = (int)c32.g,
                    ["b"] = (int)c32.b,
                    ["a"] = (int)c32.a
                };
            }

            if (value is Rect rect)
            {
                return new JSONObject { ["x"] = rect.x, ["y"] = rect.y, ["width"] = rect.width, ["height"] = rect.height };
            }

            if (value is Bounds bounds)
            {
                return new JSONObject
                {
                    ["center"] = new JSONObject { ["x"] = bounds.center.x, ["y"] = bounds.center.y, ["z"] = bounds.center.z },
                    ["size"] = new JSONObject { ["x"] = bounds.size.x, ["y"] = bounds.size.y, ["z"] = bounds.size.z }
                };
            }

            if (value is LayerMask mask)
            {
                return mask.value;
            }

            if (value is UnityEngine.Object obj)
            {
                var json = new JSONObject();
                json["instanceId"] = obj.GetInstanceID();
                json["name"] = obj.name;
                json["type"] = obj.GetType().FullName;

                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    json["assetPath"] = path;
                }

                return json;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                var arr = new JSONArray();
                foreach (var item in enumerable)
                {
                    arr.Add(SerializeValue(item));
                }
                return arr;
            }

            return value.ToString();
        }

        private object ParseUnityObject(JSONNode node, Type targetType)
        {
            if (node == null || node.IsNull)
            {
                return null;
            }

            if (node.IsObject)
            {
                var objNode = node.AsObject;

                if (objNode.HasKey("instanceId"))
                {
#pragma warning disable CS0618
                    var byId = EditorUtility.InstanceIDToObject(objNode["instanceId"].AsInt);
#pragma warning restore CS0618
                    if (byId != null && targetType.IsInstanceOfType(byId))
                    {
                        return byId;
                    }
                }

                if (objNode.HasKey("assetPath"))
                {
                    var byPath = AssetDatabase.LoadAssetAtPath(objNode["assetPath"].Value, targetType);
                    if (byPath != null)
                    {
                        return byPath;
                    }
                }
            }

            if (!string.IsNullOrEmpty(node.Value))
            {
                if (int.TryParse(node.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                {
#pragma warning disable CS0618
                    var byId = EditorUtility.InstanceIDToObject(id);
#pragma warning restore CS0618
                    if (byId != null && targetType.IsInstanceOfType(byId))
                    {
                        return byId;
                    }
                }

                var byPath = AssetDatabase.LoadAssetAtPath(node.Value, targetType);
                if (byPath != null)
                {
                    return byPath;
                }

                var byGuidPath = AssetDatabase.GUIDToAssetPath(node.Value);
                if (!string.IsNullOrEmpty(byGuidPath))
                {
                    var byGuid = AssetDatabase.LoadAssetAtPath(byGuidPath, targetType);
                    if (byGuid != null)
                    {
                        return byGuid;
                    }
                }
            }

            return null;
        }

        private long ParseLong(JSONNode node)
        {
            if (node == null || node.IsNull)
            {
                return 0L;
            }

            if (long.TryParse(node.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out long value))
            {
                return value;
            }

            return Convert.ToInt64(node.AsDouble);
        }

        private char ParseChar(JSONNode node)
        {
            string text = node == null ? string.Empty : node.Value;
            if (string.IsNullOrEmpty(text))
            {
                return '\0';
            }

            return text[0];
        }
    }
}
