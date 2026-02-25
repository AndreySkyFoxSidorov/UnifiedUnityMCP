using System;
using Mcp.Editor.Commands;
using Mcp.Editor.Tools;
using Mcp.Editor.Transport;
using Mcp.Editor.Util;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Mcp.Editor
{
    /// <summary>
    /// Main entry point for the Unity MCP Server implementation.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMcpServer
    {
        private static StreamableHttpTransport _transport;
        private static bool _isRunning;
        public static bool IsRunning => _isRunning;

        public static void Start()
        {
            if (_isRunning) return;

            MainThreadDispatcher.Initialize();

            // Setup commands
            CommandRegistry.Clear();
            CommandRegistry.Register(new InitializeCommand());
            CommandRegistry.Register(new NotificationsInitializedCommand());
            CommandRegistry.Register(new ToolsListCommand());
            CommandRegistry.Register(new ToolsCallCommand());

            // Setup tools
            ToolRegistry.Clear();
            ToolRegistry.Register(new UnityPingTool());
            ToolRegistry.Register(new ConsoleGetLogsTool());
            ToolRegistry.Register(new EditorGetStateTool());
            ToolRegistry.Register(new EditorSetStateTool());
            ToolRegistry.Register(new EditorSelectionTool());
            ToolRegistry.Register(new GameObjectManageTool());
            ToolRegistry.Register(new ComponentManageTool());
            ToolRegistry.Register(new AssetManageTool());
            ToolRegistry.Register(new PrefabManageTool());
            ToolRegistry.Register(new AssetMetaManageTool());

            // Advanced tools
            ToolRegistry.Register(new ComponentPropertyTool());
            ToolRegistry.Register(new SceneManageTool());
            ToolRegistry.Register(new AssetCreateTool());
            ToolRegistry.Register(new ExecuteMenuTool());
            ToolRegistry.Register(new TestRunTool());
            ToolRegistry.Register(new BuildManageTool());

            // Skill module tools
            foreach (var moduleTool in UnitySkillModuleTools.CreateAll())
            {
                ToolRegistry.Register(moduleTool);
            }

            // Start transport
            _transport = new StreamableHttpTransport("http://127.0.0.1:18008/mcp/", "/mcp");
            _transport.OnMessageReceived = CommandRegistry.HandleRequest;
            _transport.Start();

            _isRunning = true;
        }

        public static void Stop()
        {
            if (!_isRunning) return;

            _transport?.Stop();
            _transport = null;

            MainThreadDispatcher.Shutdown();
            Mcp.Editor.Protocol.McpSession.ClearAll();

            _isRunning = false;
        }

        // Backward compatibility for toolbar toggles that try to hook to update
        public static void UpdateDispatcher()
        {
        }
    }
}
