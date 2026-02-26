# Unified Unity MCP Tools Reference

This document lists all 52 tools (16 Core + 36 Module) available in the Unified Unity MCP Server.

## Core Tools

### "unity_ping"
- **Description**: Returns pong and current Unity version

### "unity_console_read"
- **Description**: Reads Unity Editor Console logs

### "unity_editor_state"
- **Description**: Returns current Editor play mode state

### "unity_editor_set_state"
- **Description**: Sets the editor state (play, pause, stop)

### "unity_selection_get"
- **Description**: Gets currently selected objects

### "unity_gameobject_manage"
- **Description**: GameObject find/create/destroy actions

### "unity_component_manage"
- **Description**: Component add/remove/list actions

### "unity_asset_manage"
- **Description**: Asset find/refresh actions

### "unity_prefab_instantiate"
- **Description**: Instantiate prefab asset into active scene

### "unity_asset_meta"
- **Description**: Read/write importer properties via SerializedProperty

### "unity_component_property"
- **Description**: Reflection get/set/dump/invoke for component members

### "unity_scene_manage"
- **Description**: Scene open/save/new/list_build_scenes actions

### "unity_asset_create"
- **Description**: Create folder/material assets

### "unity_editor_execute_menu"
- **Description**: Execute Unity Editor menu item

### "unity_test_run"
- **Description**: Run Unity Test Runner (EditMode/PlayMode)

### "unity_build_manage"
- **Description**: Get/set defines and run build action

## Module Tools

Module tools follow the standard envelope format using `action` argument. Call `action: "list_actions"` to see precise capabilities for each module.

- **"unity_animator"**: Module tool for animator operations.
- **"unity_asset"**: Module tool for asset operations.
- **"unity_bookmark"**: Module tool for bookmark operations.
- **"unity_camera"**: Module tool for camera operations.
- **"unity_cinemachine"**: Module tool for cinemachine operations.
- **"unity_cleaner"**: Module tool for cleaner operations.
- **"unity_component"**: Module tool for component operations.
- **"unity_console"**: Module tool for console operations.
- **"unity_debug"**: Module tool for debug operations.
- **"unity_editor"**: Module tool for editor operations.
- **"unity_event"**: Module tool for event operations.
- **"unity_gameobject"**: Module tool for gameobject operations.
- **"unity_history"**: Module tool for history operations.
- **"unity_importer"**: Module tool for importer operations.
- **"unity_light"**: Module tool for light operations.
- **"unity_material"**: Module tool for material operations.
- **"unity_navmesh"**: Module tool for navmesh operations.
- **"unity_optimization"**: Module tool for optimization operations.
- **"unity_package"**: Module tool for package operations.
- **"unity_perception"**: Module tool for perception operations.
- **"unity_physics"**: Module tool for physics operations.
- **"unity_prefab"**: Module tool for prefab operations.
- **"unity_profiler"**: Module tool for profiler operations.
- **"unity_project"**: Module tool for project operations.
- **"unity_sample"**: Module tool for sample operations.
- **"unity_scene"**: Module tool for scene operations.
- **"unity_script"**: Module tool for script operations.
- **"unity_scriptableobject"**: Module tool for scriptableobject operations.
- **"unity_shader"**: Module tool for shader operations.
- **"unity_smart"**: Module tool for smart operations.
- **"unity_terrain"**: Module tool for terrain operations.
- **"unity_test"**: Module tool for test operations.
- **"unity_timeline"**: Module tool for timeline operations.
- **"unity_ui"**: Module tool for ui operations.
- **"unity_validation"**: Module tool for validation operations.
- **"unity_workflow"**: Module tool for workflow operations.

