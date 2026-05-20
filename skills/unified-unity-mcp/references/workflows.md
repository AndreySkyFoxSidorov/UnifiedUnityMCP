# Unified Unity MCP Workflows

Use this document as workflow background only. Verify tool names and arguments against `active_tools.json`, `ToolsCatalog.md`, or live `tools/list` before relying on examples.

## Scene Inspection And Modification

### Modify A Light's Intensity

1. Check editor state:
   ```json
   unity_editor_state({})
   ```
2. Find the target object:
   ```json
   unity_gameobject_manage({ "action": "find", "name": "Directional Light" })
   ```
3. List components on the returned GameObject `instanceId`:
   ```json
   unity_component_manage({ "action": "list", "instanceId": 123 })
   ```
4. Set the Light component property:
   ```json
   unity_component_property({
     "action": "set",
     "instanceId": 123,
     "componentType": "UnityEngine.Light",
     "property": "intensity",
     "value": 1.5
   })
   ```
5. Re-read the property or console output before reporting completion.

## Asset Generation And Configuration

### Create And Assign A Material

1. Create the material asset:
   ```json
   unity_asset_create({
     "action": "material",
     "path": "Assets/Materials/EnemyMat.mat",
     "shader": "Standard"
   })
   ```
2. Read console output:
   ```json
   unity_console_read({ "maxLines": 20 })
   ```
3. Find the target object, then assign the material to a renderer:
   ```json
   unity_component_property({
     "action": "set",
     "instanceId": 123,
     "componentType": "UnityEngine.MeshRenderer",
     "property": "sharedMaterial",
     "value": "Assets/Materials/EnemyMat.mat"
   })
   ```

## Script Creation And Attachment

1. Write or edit the C# file through normal filesystem tools.
2. Refresh the Asset Database:
   ```json
   unity_asset_manage({ "action": "refresh" })
   ```
3. Poll `unity_editor_state` until `isCompiling` is false.
4. Read recent console output:
   ```json
   unity_console_read({ "maxLines": 50 })
   ```
5. Create or find the GameObject, then attach the component:
   ```json
   unity_gameobject_manage({ "action": "create", "name": "Player" })
   unity_component_manage({
     "action": "add",
     "instanceId": 123,
     "componentType": "PlayerController"
   })
   ```

## Pre-Build Testing

1. Run the narrowest useful test set:
   ```json
   unity_test_run({ "mode": "editmode" })
   ```
2. Update defines only when the task requires it:
   ```json
   unity_build_manage({
     "action": "set_defines",
     "defines": "RELEASE_BUILD;NO_DEBUG"
   })
   ```
3. Build with an explicit project-relative output path:
   ```json
   unity_build_manage({
     "action": "build_player",
     "buildTarget": "StandaloneWindows64",
     "buildPath": "Builds/App"
   })
   ```
4. Read the build result payload and recent console output before reporting success.

## Bulk Selection Operations

1. Fetch selection:
   ```json
   unity_selection_get({})
   ```
2. For each returned `instanceId`, apply the narrowest matching tool.
3. Use `unity_component_property` for component values and `unity_gameobject_manage({ "action": "destroy", "instanceId": 123 })` only for confirmed destructive scopes.
