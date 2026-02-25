---
name: unified-unity-mcp
description: Operate Unity Editor through Unified Unity MCP core + module tools.
metadata:
  language: en
  domain: mcp
  ui: orchestrator
  version: "1.1.0"
---

# Unified Unity MCP Operator Guide

Unified Unity MCP now exposes:
- **16 core tools** (direct tool implementations)
- **36 module tools** (one per `doc/unity-skills-index/*` folder)

Registration entrypoint: [UnityMcpServer.Start()](Assets/ThirdParty/UnifiedUnityMCP/Editor/UnityMcpServer.cs:22)

## Transport & Protocol

- Streamable HTTP transport: [StreamableHttpTransport](Assets/ThirdParty/UnifiedUnityMCP/Editor/Transport/StreamableHttpTransport.cs:13)
- MCP endpoint: `/mcp` (`POST`, `GET`, optional `DELETE`)
- Session header: `Mcp-Session-Id`
- Initialize protocol version: `2025-03-26` in [InitializeCommand](Assets/ThirdParty/UnifiedUnityMCP/Editor/Commands/StandardCommands.cs:8)

## Core Tool IDs

- `unity_ping`
- `unity_console_read`
- `unity_editor_state`
- `unity_editor_set_state`
- `unity_editor_execute_menu`
- `unity_selection_get`
- `unity_gameobject_manage`
- `unity_component_manage`
- `unity_component_property`
- `unity_scene_manage`
- `unity_asset_manage`
- `unity_asset_create`
- `unity_asset_meta`
- `unity_prefab_instantiate`
- `unity_test_run`
- `unity_build_manage`

## Module Tool IDs (`doc/unity-skills-index` parity)

- `unity_animator`
- `unity_asset`
- `unity_bookmark`
- `unity_camera`
- `unity_cinemachine`
- `unity_cleaner`
- `unity_component`
- `unity_console`
- `unity_debug`
- `unity_editor`
- `unity_event`
- `unity_gameobject`
- `unity_history`
- `unity_importer`
- `unity_light`
- `unity_material`
- `unity_navmesh`
- `unity_optimization`
- `unity_package`
- `unity_perception`
- `unity_physics`
- `unity_prefab`
- `unity_profiler`
- `unity_project`
- `unity_sample`
- `unity_scene`
- `unity_script`
- `unity_scriptableobject`
- `unity_shader`
- `unity_smart`
- `unity_terrain`
- `unity_test`
- `unity_timeline`
- `unity_ui`
- `unity_validation`
- `unity_workflow`

## How to call module tools

Each module accepts a unified envelope:

```json
{
  "action": "list_actions | <module_action> | bridge",
  "tool": "<required for bridge>",
  "arguments": { }
}
```

Implementation details:
- Module runtime: [UnityModuleTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/UnitySkillModuleTools.cs:23)
- Factory and module registry: [UnitySkillModuleTools.CreateAll()](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/UnitySkillModuleTools.cs:151)

Behavior:
- `list_actions` returns documented actions with `implemented=true/false`.
- Implemented actions execute server-side handlers.
- Not yet implemented actions return `status="not_implemented"` + guidance.
- `bridge` forwards to a specified core tool.

## Practical guidance

- Prefer module tools first for skill-aligned behavior.
- If a module action is marked not implemented, use:
  1. `action="bridge"` and target an existing core tool, or
  2. direct core tool calls for low-level operations.
- Use `unity_component_property` for generic reflection (`get/set/dump/invoke`).

## References

- Tool catalog: [ToolsCatalog.md](Assets/ThirdParty/UnifiedUnityMCP/Editor/ToolsCatalog.md)
- Active tool manifest: [active_tools.json](Assets/ThirdParty/UnifiedUnityMCP/Editor/active_tools.json)
- Legacy tool reference (core semantics): [.agent/skills/unified-unity-mcp/references/tools-reference.md](.agent/skills/unified-unity-mcp/references/tools-reference.md)
- Workflow examples (core patterns): [.agent/skills/unified-unity-mcp/references/workflows.md](.agent/skills/unified-unity-mcp/references/workflows.md)
