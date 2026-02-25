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
            initResult["protocolVersion"] = "2025-03-26";

            var serverInfo = new JSONObject();
            serverInfo["name"] = "UnityMcpServer";
            serverInfo["version"] = "2.0.0";
            initResult["serverInfo"] = serverInfo;

            var capabilities = new JSONObject();
            var toolsCapability = new JSONObject();
            toolsCapability["listChanged"] = false;
            capabilities["tools"] = toolsCapability; // Indicate tool support
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

            string cursorStr = string.Empty;
            if (request["params"] is JSONObject reqObj && reqObj.HasKey("cursor"))
            {
                cursorStr = reqObj["cursor"].Value;
            }

            int startIndex = 0;
            if (!string.IsNullOrEmpty(cursorStr))
            {
                int.TryParse(cursorStr, out startIndex);
            }

            int count = 0;
            int limit = 20;

            var allTools = ToolRegistry.GetAllTools();
            int totalTools = 0;
            foreach (var _ in allTools) totalTools++;

            int i = 0;
            foreach (var tool in allTools)
            {
                if (i >= startIndex && count < limit)
                {
                    var toolJson = new JSONObject();
                    toolJson["name"] = tool.Name;
                    toolJson["description"] = tool.Description;
                    toolJson["inputSchema"] = tool.InputSchema;
                    toolsArray.Add(toolJson);
                    count++;
                }
                i++;
            }

            toolsResult["tools"] = toolsArray;

            if (startIndex + count < totalTools)
            {
                toolsResult["nextCursor"] = (startIndex + count).ToString();
            }

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
                bool isCompleted = false;
                object lockObj = new object();

                var timer = new System.Threading.Timer((state) =>
                {
                    lock (lockObj)
                    {
                        if (!isCompleted)
                        {
                            isCompleted = true;
                            sendError(JsonRpc.InternalError, $"Tool execution timed out ({toolName}) after 30 seconds");
                        }
                    }
                }, null, 30000, System.Threading.Timeout.Infinite);

                Action<JSONObject> wrappedResponse = (r) =>
                {
                    lock (lockObj) { if (isCompleted) return; isCompleted = true; timer.Dispose(); }
                    sendResponse(r);
                };

                Action<string> wrappedError = (msg) =>
                {
                    lock (lockObj) { if (isCompleted) return; isCompleted = true; timer.Dispose(); }
                    sendError(JsonRpc.InternalError, msg);
                };

                try
                {
                    tool.Execute(toolArgs, wrappedResponse, errMsg => wrappedError(errMsg));
                }
                catch (Exception e)
                {
                    Util.Logging.LogException(e, $"Tool Execution ({toolName})");
                    wrappedError($"Error executing tool {toolName}: {e.Message}");
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
