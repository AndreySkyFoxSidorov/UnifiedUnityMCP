using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class EditorSelectionTool : ITool
    {
        public string Name => "unity_selection_get";
        public string Description => "Gets the currently selected objects in the Unity Editor.";

        public JSONObject InputSchema => McpMessages.CreateToolSchema(Name, Description, new JSONObject());

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            MainThreadDispatcher.InvokeAsync(() =>
            {
                var arr = new JSONArray();
                foreach (var obj in Selection.objects)
                {
                    var item = new JSONObject();
                    item["name"] = obj.name;
                    item["instanceId"] = obj.GetInstanceID();
                    item["type"] = obj.GetType().Name;

                    var path = AssetDatabase.GetAssetPath(obj);
                    if (!string.IsNullOrEmpty(path))
                        item["assetPath"] = path;

                    arr.Add(item);
                }

                var result = new JSONObject();
                result["selection"] = arr;

                sendResponse(McpMessages.CreateToolResult(result.ToString()));
            });
        }
    }
}
