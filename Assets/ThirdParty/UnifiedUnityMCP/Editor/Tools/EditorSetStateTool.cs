using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class EditorSetStateTool : ITool
    {
        public string Name => "unity.editor.set_state";
        public string Description => "Sets the editor state (play, pause, stop).";

        public JSONObject InputSchema
        {
            get
            {
                var props = new JSONObject();
                props["state"] = McpMessages.CreateStringProperty("Desired state: 'play', 'pause', or 'stop'.");
                
                var required = new JSONArray();
                required.Add("state");
                
                return McpMessages.CreateToolSchema(Name, Description, props, required);
            }
        }

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            string state = arguments.GetString("state");
            if (string.IsNullOrEmpty(state))
            {
                sendError("Missing required parameter: 'state'");
                return;
            }

            MainThreadDispatcher.InvokeAsync(() =>
            {
                try
                {
                    switch (state.ToLower())
                    {
                        case "play":
                            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
                            if (EditorApplication.isPaused) EditorApplication.isPaused = false;
                            break;
                        case "pause":
                            if (EditorApplication.isPlaying) EditorApplication.isPaused = true;
                            break;
                        case "stop":
                            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                            break;
                        default:
                            sendError($"Invalid state: {state}. Use 'play', 'pause', or 'stop'.");
                            return;
                    }

                    var result = new JSONObject();
                    result["isPlaying"] = EditorApplication.isPlaying;
                    result["isPaused"] = EditorApplication.isPaused;
                    sendResponse(McpMessages.CreateToolResult(result.ToString()));
                }
                catch (Exception e)
                {
                    sendError($"Failed to set state: {e.Message}");
                }
            });
        }
    }
}
