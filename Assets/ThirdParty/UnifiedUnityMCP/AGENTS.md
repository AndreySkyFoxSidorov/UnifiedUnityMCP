# Unified Unity MCP Package Rules

## Scope

Rules for `Assets/ThirdParty/UnifiedUnityMCP`.

## Package Shape

- This subtree is the Unity-side MCP package.
- Runtime/editor source lives under `Editor/`.
- Keep generated or user-facing package docs beside the package only when they describe the package itself.
- Do not put Codex, Gemini, or Antigravity client-only rules here. Those belong at repo root, `.agent`, `.gemini`, or `skills`.

## Architecture

- Transport handles HTTP/SSE and JSON-RPC framing.
- Protocol classes define MCP and JSON-RPC messages.
- Commands route protocol methods.
- Tools implement granular Unity Editor actions.
- Util contains small shared editor helpers.

Keep this separation direct and lightweight. Do not add manager layers, broad service hubs, or generic utility dumping grounds.

## Public Surface

Treat these as one public contract surface:

- MCP tool names
- Tool argument schemas
- Tool response fields and status values
- Protocol version and initialize behavior
- Tool catalog documentation
- Skills that instruct agents to call those tools

When one changes, review and update the others in the same task.
