using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class ExecuteMenuTool : ITool
    {
        public string Name => "unity_editor_execute_menu";
        public string Description => "Executes a specified menu item from the Unity Editor's top menu.";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["menuPath"] = McpMessages.CreateStringProperty("The menu path to execute (e.g. 'Assets/Create/Folder', 'GameObject/Create Empty').");

                var required = new JSONArray();
                required.Add("menuPath");

                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string menuPath = arguments.GetString("menuPath");

            if (string.IsNullOrEmpty(menuPath))
            {
                sendError("Missing parameter: 'menuPath'");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    bool success = EditorApplication.ExecuteMenuItem(menuPath);
                    if (success)
                    {
                        sendResponse(McpMessages.CreateToolResult($"{{\"status\":\"Success\", \"menuPath\":\"{menuPath}\"}}"));
                    }
                    else
                    {
                        sendError($"Failed to execute menu item '{menuPath}'. Note: Unity logs a native error stacktrace (MenuController::ExecuteMainMenuItem) to the console when a menu path is invalid. Ensure the path is exactly as it appears in the editor top bar (e.g., 'Assets/Create/Folder').");
                    }
                }
                catch (Exception e)
                {
                    sendError($"Execute menu failed: {e.Message}");
                }
            });
        }
    }
}
