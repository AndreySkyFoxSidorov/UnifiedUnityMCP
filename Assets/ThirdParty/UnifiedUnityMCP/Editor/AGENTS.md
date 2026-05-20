# Editor Server Rules

## Scope

Rules for `Assets/ThirdParty/UnifiedUnityMCP/Editor`.

## Code Organization

- `Transport/` owns HTTP, SSE, request validation, sessions, and response writing.
- `Protocol/` owns JSON-RPC and MCP message DTOs.
- `Commands/` owns MCP method routing.
- `Tools/` owns Unity Editor tool implementations.
- `Util/` owns small shared helpers used by the editor server.
- `Tests/` owns Unity Test Framework coverage.

## Tool Implementation

- Keep each tool focused on one MCP action family.
- Keep argument parsing explicit and close to execution.
- Marshal Unity API calls onto the main thread when needed.
- Validate external input before touching Unity objects, assets, scenes, or build settings.
- Return structured errors instead of throwing through the protocol boundary.
- Do not use `switch`.
- Do not add reflection unless the tool explicitly needs generic Unity component access.

## Catalog Sync

When adding, removing, renaming, or changing a tool:

- Update registration code.
- Update `active_tools.json` if it is checked in as the current catalog snapshot.
- Update `ToolsCatalog.md`.
- Update `skills/unified-unity-mcp/SKILL.md` and references when agent workflow changes.
- Update README snippets if public setup or examples change.
- Add or update the narrowest useful EditMode tests or smoke validation.

## Safety

- Editor-only code belongs here and may use `UnityEditor`.
- Do not move editor code into runtime assemblies.
- Do not hand-edit serialized asset metadata when a Unity API path is available.
- Do not leave temporary editor scripts or one-off test output files behind.
