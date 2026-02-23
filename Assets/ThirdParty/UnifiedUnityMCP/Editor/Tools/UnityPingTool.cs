using System;
using Mcp.Editor.Protocol;
using SimpleJSON;
using UnityEngine;

namespace Mcp.Editor.Tools
{
    public class UnityPingTool : ITool
    {
        public string Name => "unity.ping";
        public string Description => "Returns pong and current Unity version";
        public JSONObject InputSchema => McpMessages.CreateToolSchema(Name, Description, new JSONObject());

        public void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError)
        {
            try
            {
                string resultText = $"pong! Unity Version: {Application.unityVersion}, Platform: {Application.platform}";
                sendResponse(McpMessages.CreateToolResult(resultText));
            }
            catch (Exception e)
            {
                sendError($"Ping failed: {e.Message}");
            }
        }
    }
}
