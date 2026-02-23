using SimpleJSON;
using System;
using System.Collections.Generic;

namespace Mcp.Editor.Commands
{
    public interface ICommand
    {
        string Name { get; }
        
        /// <summary>
        /// Executes the JSON-RPC command.
        /// <param name="request">The full JSON request object</param>
        /// <param name="sendResponse">Callback to send a successful result</param>
        /// <param name="sendError">Callback to send an error (code, message)</param>
        /// </summary>
        void Execute(JSONObject request, Action<JSONObject> sendResponse, Action<int, string> sendError);
    }

    public static class CommandRegistry
    {
        private static readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

        public static void Register(ICommand command)
        {
            _commands[command.Name] = command;
        }

        public static void Clear()
        {
            _commands.Clear();
        }

        public static void HandleRequest(JSONObject request, Action<JSONObject> sendResponse, Action<int, string> sendError)
        {
            string method = request["method"]?.Value;
            
            if (string.IsNullOrEmpty(method))
            {
                sendError(-32600, "Invalid Request");
                return;
            }

            if (_commands.TryGetValue(method, out var command))
            {
                try
                {
                    command.Execute(request, sendResponse, sendError);
                }
                catch (Exception e)
                {
                    Util.Logging.LogException(e, $"Command {method}");
                    sendError(-32603, $"Internal Server Error: {e.Message}");
                }
            }
            else
            {
                sendError(-32601, $"Method not found: {method}");
            }
        }
    }
}
