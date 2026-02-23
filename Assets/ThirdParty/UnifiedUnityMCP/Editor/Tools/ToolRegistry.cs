using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace Mcp.Editor.Tools
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        
        /// <summary>
        /// JSON Schema definition for this tool's input.
        /// </summary>
        JSONObject InputSchema { get; }

        /// <summary>
        /// Executes the tool logic.
        /// <param name="arguments">The params/arguments from the request</param>
        /// <param name="sendResponse">Callback to return tool result content array</param>
        /// <param name="sendError">Callback to return an error string</param>
        /// </summary>
        void Execute(JSONObject arguments, Action<JSONObject> sendResponse, Action<string> sendError);
    }

    public static class ToolRegistry
    {
        private static readonly Dictionary<string, ITool> _tools = new Dictionary<string, ITool>();

        public static void Register(ITool tool)
        {
            _tools[tool.Name] = tool;
        }

        public static void Clear()
        {
            _tools.Clear();
        }

        public static List<ITool> GetAllTools()
        {
            return _tools.Values.ToList();
        }

        public static bool TryGetTool(string name, out ITool tool)
        {
            if (string.IsNullOrEmpty(name))
            {
                tool = null;
                return false;
            }
            return _tools.TryGetValue(name, out tool);
        }
    }
}
