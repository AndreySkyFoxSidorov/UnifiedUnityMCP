# Agent Client Config

This folder contains optional local client configuration only.

- `mcp.json` points an agent client at the Unity MCP endpoint.
- Active project rules live in the repository root `AGENTS.md`.
- Active reusable skills live in the repository root `skills/`.
- Do not add long-lived rules or skills under `.agent/`.

Default endpoint:

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
