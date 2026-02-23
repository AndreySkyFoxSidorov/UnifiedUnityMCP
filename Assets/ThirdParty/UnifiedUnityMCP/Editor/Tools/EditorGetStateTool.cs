using System;
using Mcp.Editor.Protocol;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;

namespace Mcp.Editor.Tools
{
    public class EditorGetStateTool : ITool
    {
        public string Name => "unity.editor.state";
        public string Description => "Returns current Editor play mode state (isPlaying, isPaused, isCompiling).";

        public JSONObject InputSchema => McpMessages.CreateToolSchema(Name, Description, new JSONObject());

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            MainThreadDispatcher.InvokeAsync(() =>
            {
                var result = new JSONObject();
                result["isPlaying"] = EditorApplication.isPlaying;
                result["isPaused"] = EditorApplication.isPaused;
                result["isCompiling"] = EditorApplication.isCompiling;
                
                sendResponse(McpMessages.CreateToolResult(result.ToString()));
            });
        }
    }
}
