using System;
using SimpleJSON;
using Mcp.Editor.Protocol;
using Mcp.Editor.Tools;

namespace Mcp.Editor.Commands
{
    public class InitializeCommand : ICommand
    {
        public string Name => "initialize";

        public void Execute(JSONObject request, Action<JSONObject> sendResponse, Action<int, string> sendError)
        {
            var initResult = new JSONObject();
            initResult["protocolVersion"] = "2024-11-05";

            var serverInfo = new JSONObject();
            serverInfo["name"] = "UnityMcpServer";
            serverInfo["version"] = "2.0.0";
            initResult["serverInfo"] = serverInfo;

            var capabilities = new JSONObject();
            capabilities["tools"] = new JSONObject(); // Indicate tool support
            initResult["capabilities"] = capabilities;

            sendResponse(initResult);
        }
    }

    public class NotificationsInitializedCommand : ICommand
    {
        public string Name => "notifications/initialized";

        public void Execute(JSONObject request, Action<JSONObject> sendResponse, Action<int, string> sendError)
        {
            // Just respond OK to the JSON-RPC
            // Technically it's a notification so the client might not expect an ID response, but returning null or empty object is fine.
            sendResponse(new JSONObject());
        }
    }

    public class ToolsListCommand : ICommand
    {
        public string Name => "tools/list";

        public void Execute(JSONObject request, Action<JSONObject> sendResponse, Action<int, string> sendError)
        {
            var toolsResult = new JSONObject();
            var toolsArray = new JSONArray();

            foreach (var tool in ToolRegistry.GetAllTools())
            {
                var toolJson = new JSONObject();
                toolJson["name"] = tool.Name;
                toolJson["description"] = tool.Description;
                toolJson["inputSchema"] = tool.InputSchema;
                toolsArray.Add(toolJson);
            }

            toolsResult["tools"] = toolsArray;
            sendResponse(toolsResult);
        }
    }

    public class ToolsCallCommand : ICommand
    {
        public string Name => "tools/call";

        public void Execute(JSONObject request, Action<JSONObject> sendResponse, Action<int, string> sendError)
        {
            var paramsNode = request["params"] as JSONObject;
            if (paramsNode == null)
            {
                sendError(JsonRpc.InvalidParams, "Missing params object");
                return;
            }

            string toolName = paramsNode["name"]?.Value;
            var toolArgs = paramsNode["arguments"] as JSONObject;

            if (ToolRegistry.TryGetTool(toolName, out ITool tool))
            {
                try
                {
                    tool.Execute(toolArgs, sendResponse, errMsg => sendError(JsonRpc.InternalError, errMsg));
                }
                catch (Exception e)
                {
                    Util.Logging.LogException(e, $"Tool Execution ({toolName})");
                    sendError(JsonRpc.InternalError, $"Error executing tool {toolName}: {e.Message}");
                }
            }
            else
            {
                // Return explicitly defined JSON-RPC level error for missing method/tool
                sendError(JsonRpc.InvalidParams, $"Tool not found: {toolName}");
            }
        }
    }
}
