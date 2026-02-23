# Unified Unity MCP Workflows

This document outlines common patterns and workflows specifically tailored to the Unified Unity MCP Server's native `unity.*` tool endpoints.

## Scene Introspection & Modification

### Modifying a Light's Color or Intensity
1. Check Editor State:
   ```json
   unity.editor.state({})
   ```
2. Find the Light:
   ```json
   unity.gameobject.find({ "name": "Directional Light" })
   ```
3. Dump light properties to confirm field names:
   ```json
   unity.component.property({
       "action": "dump",
       "instanceId": <result_from_find>
   })
   ```
4. Find the `Light` component properties (e.g. `m_Color` or standard reflected `color` and `intensity` properties) and modify them:
   ```json
   unity.component.property({
       "action": "set",
       "instanceId": <result_from_find>,
       "property": "intensity",
       "value": 1.5
   })
   ```
5. Confirm visual changes via the Editor.

## Asset Generation & Configuration Flow

### Creating and Configuring a Material
1. Create a basic material asset:
   ```json
   unity.asset.create({
       "action": "material",
       "path": "Assets/Materials/EnemyMat.mat",
       "shader": "Standard"
   })
   ```
2. Check `unity.console.read` to ensure there were no creation errors.
3. Apply the material to an object:
   ```json
   unity.gameobject.find({ "name": "Enemy" })
   unity.component.property({
       "action": "set",
       "instanceId": <enemy_id>,
       "property": "sharedMaterial", // Assuming MeshRenderer
       "value": "Assets/Materials/EnemyMat.mat" // Or GUID
   })
   ```

## Script Creation and Attachment Playbook

The Unified MCP Server expects you to handle C# generation through native filesystem interactions (`write_to_file` tool from your own system environment), followed by Unity triggers.

1. Write C# File:
   - Use standard `write_to_file` tool to save `Assets/Scripts/PlayerController.cs`.
2. Notify Unity to Refresh:
   ```json
   unity.asset.refresh({})
   ```
3. Poll `unity.editor.state` until `isCompiling` becomes `false`.
4. Check `unity.console.read(maxLines=20, typeFilter="Error")` to ensure compilation succeeded.
5. Create GameObject and Attach:
   ```json
   unity.gameobject.create({ "name": "Player" })
   unity.component.add({ "instanceId": <new_id>, "componentType": "PlayerController" })
   ```

## Pre-Build Testing Flow

Checking automated tests and making a build directly within the editor:

1. Validate Tests:
   ```json
   unity.test.run({ "mode": "editmode" })
   // Wait for response indicating 0 failures.
   ```
2. Enforce Compile Settings:
   ```json
   unity.build.manage({
       "action": "set_defines",
       "defines": "RELEASE_BUILD;NO_DEBUG"
   })
   ```
3. Create Build:
   ```json
   unity.build.manage({
       "action": "build_player",
       "buildTarget": "StandaloneWindows64"
   })
   ```
4. Read final Build Report payload to confirm output path and 0 errors.

## Bulk Selection Operations
If a user asks "Perform X on the objects I have selected":
1. Fetch selection:
   ```json
   unity.selection.get({})
   ```
2. Loop over the returned `instanceId` array internally in your thought process.
3. Dispatch `unity.component.property` or `unity.gameobject.destroy` tool calls iteratively over those IDs.
