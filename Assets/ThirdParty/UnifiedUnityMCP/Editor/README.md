# Unity MCP Server

A unified implementation of the Model Context Protocol (MCP) server for Unity using the Streamable HTTP Transport (Protocol Revision 2025-03-26).

## Architecture

This project was built from scratch using clean KISS architecture, avoiding third-party packages (uses Unity's `HttpListener` and `SimpleJSON`):

*   **Transport**: `StreamableHttpTransport` and `SseStream` handle standard JSON-RPC 2.0 via POST and SSE streams via GET on `/mcp`.
*   **Protocol**: Definitions for JSON-RPC structures (`McpMessages`, `JsonRpc`, `McpSession`).
*   **Commands**: Core JSON-RPC routing without switch statements (`CommandRegistry`, `InitializeCommand`, `ToolsListCommand`, `ToolsCallCommand`).
*   **Tools**: Granular actions implementing `ITool` managed by `ToolRegistry`.
*   **Util**: `MainThreadDispatcher` to route background HTTP thread commands safely onto Unity's main thread.

## Installation / Usage

The server is placed entirely inside `Assets/mcp/Editor`.
It is configured to auto-start and runs on **http://127.0.0.1:18008/mcp**.

### Editor Controls
A toolbar toggle button labeled **MCP** is automatically injected into the Unity Play Toolbar. 
*   **Green**: Server is running.
*   **Default**: Server is stopped.

You can also control the server via the top menu:
*   `MCP` -> `Start Server`
*   `MCP` -> `Stop Server`
*   `MCP` -> `Run Smoke Test` (Executes a local validation sequence checking `initialize`, `tools/list`, and `tools/call`)

> [!NOTE]
> **Platform Support**: `HttpListener` works reliably in Unity Editor (Windows/macOS/Linux) and Standalone desktop builds. It is **not supported** natively on WebGL or mobile without custom low-level socket programming. For Editor productivity, this implementation is fully cross-platform.

## Example Curl Tests

To test the server manually from your terminal, try these basic requests:

### 1. SSE Connection (GET)
Open a stream to receive lifecycle events and `Mcp-Session-Id`:
```bash
curl -N http://127.0.0.1:18008/mcp
```

### 2. Initialize (POST)
In a separate terminal, test the initialize handshake (using JSON-RPC 2.0):
```bash
curl -X POST http://127.0.0.1:18008/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": { "protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "curl", "version": "1.0"} }}'
```

### 3. List Tools (POST)
```bash
curl -X POST http://127.0.0.1:18008/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc": "2.0", "id": 2, "method": "tools/list"}'
```

### 4. Call a Tool (POST)
```bash
curl -X POST http://127.0.0.1:18008/mcp \
     -H "Content-Type: application/json" \
     -d '{"jsonrpc": "2.0", "id": 3, "method": "tools/call", "params": {"name": "unity.ping", "arguments": {}}}'
```
