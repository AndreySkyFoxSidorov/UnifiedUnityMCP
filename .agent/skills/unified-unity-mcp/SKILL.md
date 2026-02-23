---
name: unified-unity-mcp
description: Orchestrate Unity Editor via the Unified MCP (Model Context Protocol) server. Use this skill when working with the Unity project via MCP—creating/modifying GameObjects, editing components, managing scenes, running tests, or performing Editor automation. Provides best practices, tool schemas, and workflow patterns for effective integration.
metadata:
  language: en
  domain: mcp
  ui: orchestrator
  version: "1.0.1"
---

# Unified Unity MCP Operator Guide

This skill helps you effectively drive the Unity Editor using the Unified MCP tool suite. The unified server provides 19 highly granular, exactingly typed tools spanning GameObjects, Attributes, Assets, Scenes, Editor State, and Tests.

## Template Notice

Examples in `references/workflows.md` and `references/tools-reference.md` are generic templates. Adapt names, exact component paths, and properties to the specific project hierarchy.

Before applying a template:
- Validate targets by using `unity.gameobject.find` or `unity_selection_get`.
- Verify Editor states via `unity_editor_state` before pushing major code chunks or initiating playmode.

## Quick Start: Verification-First Workflow

**Always verify state and existence before modifying.**

```
1. Check editor state     → unity_editor_state (ensure we aren't compiling or playing)
2. Find what you need     → unity.gameobject.find or unity.asset.find
3. Read current values    → unity_component_property (action="dump") or unity_asset_meta (action="dump")
4. Take action            → unity.gameobject.create, unity_component_property (action="set"), unity_prefab_instantiate
5. Verify results         → unity_console_read
```

## Critical Best Practices

### 1. Check Editor State Before Complex Operations
Always ensure Unity is ready for commands, especially before running tests, entering PlayMode, or building.
```python
state = unity_editor_state()
# Ensure isCompiling == false before taking actions that depend on script changes.
```

### 2. File Editing vs In-Editor Scripting
The Unified MCP Server does *not* have a built-in `create_script` tool. You must use your standard environment tools (like `write_to_file` and `replace_file_content`) to create and modify `.cs` scripts in the `Assets/` directory.
After modifying a script:
1. Call `unity.asset.refresh()` to force Unity to compile.
2. Poll `unity_editor_state()` until `isCompiling` is false.
3. Read `unity_console_read()` to check for syntax errors.

## Complete Tools Usage Guide

Here is a detailed breakdown of how to use the specific tools in the Unified MCP server.

### 1. GameObjects & Hierarchy Navigation

*   **`unity.gameobject.find`**: Use this to find an object *before* doing anything else to it. It strictly returns arrays of matched items. Expect an array containing `instanceId`, `name`, and `scene`.
    *   *Example payload*: `{"name": "Player", "tag": "Player"}`
    *   *Usage*: Focus on the returned `"instanceId"` for subsequent mutations.
*   **`unity.gameobject.create`**: Instantiates a new object in the hierarchy.
    *   *Example payload*: `{"name": "Enemy", "primitiveType": "Cube", "parentRoute": "Level/Enemies"}`
    *   *Usage*: Use `primitiveType` for quick placeholders (Cube, Sphere, Capsule). `parentRoute` constructs or finds the parent folders in the hierarchy.
*   **`unity.gameobject.destroy`**: Deletes the object.
    *   *Example payload*: `{"instanceId": 1234}`

### 2. Components & Properties Manipulation (High Power)

*   **`unity.component.add`**: Attaches a new script or native component to an object.
    *   *Example payload*: `{"instanceId": 1234, "componentType": "UnityEngine.BoxCollider"}`
*   **`unity_component_property`**: This tool leverages deep reflection to read or modify any public/private field on a component. You must know the exact C# property name.
    *   *Dump Strategy*: First, dump all readable properties to understand exact names/types:
        `{"action": "dump", "instanceId": 1234, "property": ""}`
    *   *Set Action (Numbers/Bools/Strings)*: Note the exact name, then modify it:
        `{"action": "set", "instanceId": 1234, "property": "intensity", "value": 2.5}`
    *   *Set Action (Vectors/Colors)*: Pass structs as JSON objects.
        `{"action": "set", "instanceId": 1234, "property": "localPosition", "value": {"x":0,"y":5,"z":0}}`
        `{"action": "set", "instanceId": 1234, "property": "color", "value": {"r":1.0,"g":0,"b":0,"a":1}}`

### 3. Asset & Project Management

*   **`unity.asset.find`**: Search for assets globally using standard Unity search filters.
    *   *Example payload*: `{"filter": "t:Texture2D", "folders": ["Assets/Textures"]}`
*   **`unity_prefab_instantiate`**: Spawn a Prefab asset into the scene.
    *   *Example payload*: `{"assetPath": "Assets/Prefabs/Player.prefab", "position": {"x":10, "y":0, "z":0}}`
    *   *Usage*: Returns the `instanceId` of the new root object for further configuration.
*   **`unity_asset_create`**: Directly scaffold folders and materials without parsing YAML.
    *   *Example payload*: `{"action": "material", "path": "Assets/Materials/Red.mat", "shader": "Standard"}`
*   **`unity_asset_meta`**: Interrogate and modify Unity `.meta` (Importer) settings directly—do NOT attempt to parse YAML manually.
    *   *Dump parameters*: `{"action": "dump", "path": "Assets/Textures/Icon.png"}`
    *   *Set Setting*: `{"action": "set", "path": "Assets/Textures/Icon.png", "property": "m_TextureSettings.m_FilterMode", "value": 1}`

### 4. Scene & Selection Control

*   **`unity_scene_manage`**: Open, save, or construct scenes.
    *   *Example payload*: `{"action": "open", "path": "Assets/Scenes/Level1.unity"}`
    *   *Usage*: Always save the current scene `{"action": "save"}` before opening a new one to prevent data loss.
*   **`unity_selection_get` / `unity.selection.set`**: See what the human developer has selected in the Editor right now, or force the selection to highlight an object you just created.
    *   *Get payload*: `{}` -> Returns active `instanceId` array.
    *   *Set payload*: `{"instanceIds": [1234]}`

### 5. Editor Automation & Diagnostics

*   **`unity_editor_execute_menu`**: Clicks a button in the top menu bar.
    *   *Example payload*: `{"menuPath": "Assets/Create/Folder"}`
    *   *Usage*: Note that if the path is invalid, Unity logs a severe native stack trace. Ensure exact spelling.
*   **`unity_console_read`**: Grabs the latest log entries. Vital for debugging compilation loops.
    *   *Example payload*: `{"maxLines": 50, "typeFilter": "Error"}`
*   **`unity_test_run`**: Triggers Unity Test Runner (NUnit).
    *   *Example payload*: `{"mode": "playmode"}`
    *   *Usage*: Editor will freeze while tests run, returning a comprehensive JSON report of success/failure contexts.
*   **`unity_build_manage`**: Inject `#define` symbols or trigger project builds.
    *   *Example payload*: `{"action": "set_defines", "defines": "DEBUG;PROFILING"}`

## Reference Files

For detailed tool schemas and workflows, refer to:
- **[tools-reference.md](references/tools-reference.md)**: Master list of all 19 tool JSON schemas.
- **[workflows.md](references/workflows.md)**: Extended scenario templates for common development loops.
