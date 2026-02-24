---
name: unified-unity-mcp
description: Orchestrate Unity Editor via Unified MCP tools.
metadata:
  language: en
  domain: mcp
  ui: orchestrator
  version: "1.0.2"
---

# Unified Unity MCP Operator Guide

Unified Unity MCP currently exposes **20 tools**.

## Allowed Tool IDs

- `unity_ping`
- `unity_console_read`
- `unity_editor_state`
- `unity_editor_set_state`
- `unity_editor_execute_menu`
- `unity_selection_get`
- `unity.selection.set`
- `unity.gameobject.find`
- `unity.gameobject.create`
- `unity.gameobject.destroy`
- `unity.component.add`
- `unity_component_property`
- `unity_scene_manage`
- `unity.asset.find`
- `unity_asset_create`
- `unity_asset_meta`
- `unity.asset.refresh`
- `unity_prefab_instantiate`
- `unity_test_run`
- `unity_build_manage`


## Notes
- Use these exact tool identifiers.
- Mixed naming style is intentional: some IDs are `unity.*`, others are `unity_*`.
- `references/tools-reference.md` is the schema source of truth.
