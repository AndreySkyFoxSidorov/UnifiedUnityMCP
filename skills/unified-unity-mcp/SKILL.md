---
name: unified-unity-mcp
description: Operate and maintain the Unified Unity MCP server. Use when calling MCP tools, updating tool contracts, validating catalogs, or changing Unity Editor automation workflows.
---

# Unified Unity MCP

## Use This Skill When

- Calling Unity MCP tools from an AI client.
- Adding, renaming, removing, or changing a tool.
- Updating tool documentation, examples, or client setup.
- Debugging the MCP server, transport, commands, or tool registry.

## Sources Of Truth

- Live server `tools/list` when available.
- `Assets/ThirdParty/UnifiedUnityMCP/Editor/active_tools.json`
- `Assets/ThirdParty/UnifiedUnityMCP/Editor/ToolsCatalog.md`
- Tool registration and implementation under `Assets/ThirdParty/UnifiedUnityMCP/Editor/`

Do not invent tools, actions, arguments, or response fields.

## Core Tools

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

## Module Tools

Module tools include `unity_animator`, `unity_asset`, `unity_bookmark`, `unity_camera`, `unity_cinemachine`, `unity_cleaner`, `unity_component`, `unity_console`, `unity_debug`, `unity_editor`, `unity_event`, `unity_gameobject`, `unity_history`, `unity_importer`, `unity_light`, `unity_material`, `unity_navmesh`, `unity_optimization`, `unity_package`, `unity_perception`, `unity_physics`, `unity_prefab`, `unity_profiler`, `unity_project`, `unity_sample`, `unity_scene`, `unity_script`, `unity_scriptableobject`, `unity_shader`, `unity_smart`, `unity_terrain`, `unity_test`, `unity_timeline`, `unity_ui`, `unity_validation`, and `unity_workflow`.

Check the catalog before calling module actions. Some module actions may be bridge or not-yet-implemented paths.

## Tool Change Checklist

- Update implementation and registration.
- Update `active_tools.json` if it is the checked-in catalog snapshot.
- Update `ToolsCatalog.md`.
- Update this skill and reference docs when workflow changes.
- Update README examples if public setup or examples change.
- Add or update targeted tests or smoke checks.

## Client Config

Default endpoint:

```text
http://127.0.0.1:18008/mcp
```

Client config examples live in `.agent/mcp.json` and `.gemini/antigravity/mcp_config.json`.

## References

- `references/tools-reference.md` - tool list background
- `references/workflows.md` - workflow examples

Verify reference content against the current catalog before relying on it.
