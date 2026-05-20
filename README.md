# Unified Unity MCP

Unified Unity MCP is an open-source Model Context Protocol server for the Unity Editor.
It exposes Unity Editor operations through a local MCP endpoint so AI clients can inspect and modify Unity projects through typed tools.

Default endpoint:

```text
http://127.0.0.1:18008/mcp
```

Protocol baseline: Streamable HTTP transport with JSON-RPC 2.0 and MCP protocol version `2025-03-26`.

## Repository Layout

```text
AGENTS.md                                      Repo-wide rules for coding agents
skills/                                        Optional agent skills and workflows
.agent/mcp.json                                Optional generic agent client config
.gemini/antigravity/mcp_config.json            Optional Gemini/Antigravity client config
Assets/ThirdParty/UnifiedUnityMCP/             Unity MCP package
Assets/ThirdParty/UnifiedUnityMCP/Editor/      Editor server, commands, tools, tests
```

The old hidden `.agent/skills` and `.agent/rules` layout has been replaced:

- active rules now live in `AGENTS.md`
- active skills now live in `skills/`
- `.agent` and `.gemini` contain client config only

## Unity Package Architecture

The Unity-side server is intentionally small and direct:

- `Transport/` handles HTTP, SSE, request validation, sessions, and response writing.
- `Protocol/` defines JSON-RPC and MCP message DTOs.
- `Commands/` routes MCP methods such as `initialize`, `tools/list`, and `tools/call`.
- `Tools/` implements Unity Editor actions.
- `Util/` contains small shared editor helpers.
- `Tests/` contains Unity Test Framework coverage.

The codebase avoids unnecessary layers and keeps Unity Editor work explicit. See `AGENTS.md` before making changes.

## MCP Client Config

Generic agent client:

```json
{
  "mcpServers": {
    "unityMCP": {
      "type": "sse",
      "serverUrl": "http://127.0.0.1:18008/mcp",
      "disabled": false,
      "alwaysAllow": [],
      "disabledTools": []
    }
  }
}
```

The same default config is kept in:

- `.agent/mcp.json`
- `.gemini/antigravity/mcp_config.json`

## Tool Catalog

Canonical tool references:

- `Assets/ThirdParty/UnifiedUnityMCP/Editor/active_tools.json`
- `Assets/ThirdParty/UnifiedUnityMCP/Editor/ToolsCatalog.md`
- `skills/unified-unity-mcp/SKILL.md`

Current core tools include:

- `unity_ping`
- `unity_console_read`
- `unity_editor_state`
- `unity_editor_set_state`
- `unity_selection_get`
- `unity_gameobject_manage`
- `unity_component_manage`
- `unity_asset_manage`
- `unity_prefab_instantiate`
- `unity_asset_meta`
- `unity_component_property`
- `unity_scene_manage`
- `unity_asset_create`
- `unity_editor_execute_menu`
- `unity_test_run`
- `unity_build_manage`

The package also exposes module tools such as `unity_scene`, `unity_gameobject`, `unity_component`, `unity_asset`, `unity_ui`, `unity_physics`, `unity_navmesh`, `unity_validation`, and others. Check the catalog before calling a tool.

## Development Rules

Short version:

- read `AGENTS.md` first
- keep Unity C# simple, direct, and readable
- do not use `switch`
- do not add dependency packages casually
- keep public MCP contracts synchronized across code, catalog, README, tests, and skills
- verify documentation-only changes with search
- verify C# and MCP changes with compile, tests, or targeted smoke checks

## Useful Manual Requests

SSE connection:

```bash
curl -N http://127.0.0.1:18008/mcp
```

Initialize:

```bash
curl -X POST http://127.0.0.1:18008/mcp \
  -H "Content-Type: application/json" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-03-26\",\"capabilities\":{},\"clientInfo\":{\"name\":\"curl\",\"version\":\"1.0\"}}}"
```

List tools:

```bash
curl -X POST http://127.0.0.1:18008/mcp \
  -H "Content-Type: application/json" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\"}"
```

Call `unity_ping`:

```bash
curl -X POST http://127.0.0.1:18008/mcp \
  -H "Content-Type: application/json" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"unity_ping\",\"arguments\":{}}}"
```
