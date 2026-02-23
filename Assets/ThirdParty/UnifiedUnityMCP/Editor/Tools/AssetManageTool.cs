using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class AssetManageTool : ITool
    {
        public string Name => "unity_asset_manage";
        public string Description => "Manage assets directly. Actions: 'find', 'refresh'.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["action"] = McpMessages.CreateStringProperty("Action: 'find', 'refresh'.");
                props["filter"] = McpMessages.CreateStringProperty("Use with 'find': filter string for AssetDatabase.FindAssets.");

                var required = new JSONArray();
                required.Add("action");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string action = arguments.GetString("action")?.ToLower();

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (action == "refresh")
                    {
                        AssetDatabase.Refresh();
                        sendResponse(McpMessages.CreateToolResult("{\"status\":\"AssetDatabase refreshed\"}"));
                    }
                    else if (action == "find")
                    {
                        string filter = arguments.GetString("filter", "");
                        string[] guids = AssetDatabase.FindAssets(filter);
                        var arr = new JSONArray();
                        foreach (var g in guids)
                        {
                            var p = AssetDatabase.GUIDToAssetPath(g);
                            arr.Add(p);
                        }
                        var res = new JSONObject();
                        res["assets"] = arr;
                        sendResponse(McpMessages.CreateToolResult(res.ToString()));
                    }
                    else
                    {
                        sendError($"Invalid action '{action}'.");
                    }
                }
                catch (Exception e)
                {
                    sendError($"Asset manage failed: {e.Message}");
                }
            });
        }
    }
}
