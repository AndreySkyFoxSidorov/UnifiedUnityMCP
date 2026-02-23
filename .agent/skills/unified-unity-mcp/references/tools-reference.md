# Unified Unity MCP Tools Reference

This document lists all 19 tools available in the Unified Unity MCP Server, detailing their exact input schemas, return values, and expected behavior.

## Core Unity Info

### `unity.ping`
- **Description**: Returns "pong" and the current Unity Editor version and platform. Use to ensure connection is alive.
- **Parameters**: None `{}`
- **Returns**: `{"content": "pong! Unity Version: 6000.3.x, Platform: WindowsEditor"}`

### `unity.console.read`
- **Description**: Capture Unity Editor Console logs.
- **Parameters**: 
  - `maxLines` (optional, int): Max number of logs to read (default 50).
  - `typeFilter` (optional, string): Filter logs by type ("Error", "Warning", "Log", "Exception", "Assert").
- **Returns**: Formatted text block with log messages.

## Editor State

### `unity.editor.state`
- **Description**: Get current PlayMode and compile state of the Unity Editor. Very important to check before making large changes or executing menus.
- **Parameters**: None `{}`
- **Returns**: `{"isPlaying": false, "isPaused": false, "isCompiling": false}`

### `unity.editor.set_state`
- **Description**: Change the PlayMode state.
- **Parameters**: 
  - `state` (required, string): One of `"play"`, `"pause"`, `"stop"`.
- **Returns**: New play state status string.

### `unity.editor.execute_menu`
- **Description**: Programmatically click a menu item from the Unity Editor's top bar.
- **Parameters**:
  - `menuPath` (required, string): Exact path, e.g., `"Assets/Create/Folder"`.
- **Returns**: Success string. Note: Fails natively if the path is invalid (logs to console).

## Selection

### `unity.selection.get`
- **Description**: Retrieve an array of currently selected GameObjects or Assets.
- **Parameters**: None `{}`
- **Returns**: `{"selected": [{"name": "Cube", "instanceId": 1234, "assetPath": "null"}]}`

### `unity.selection.set`
- **Description**: Set the Unity Editor's current selection.
- **Parameters**:
  - `instanceIds` (required, int[]): Array of integer InstanceIDs.
- **Returns**: Success status.

## GameObject Management

### `unity.gameobject.find`
- **Description**: Find GameObjects in the scene.
- **Parameters**:
  - `name` (optional, string): Exact object name to find.
  - `tag` (optional, string): Tag to match.
  - *Must provide at least one filter.*
- **Returns**: `{"found": [{"name": "Player", "instanceId": 5678, "scene": "main"}]}`

### `unity.gameobject.create`
- **Description**: Create a single GameObject.
- **Parameters**:
  - `name` (required, string): Name of the new object.
  - `primitiveType` (optional, string): E.g., `"Cube"`, `"Sphere"`, `"Capsule"`.
  - `parentRoute` (optional, string): Hierarchy path to parent under.
- **Returns**: `{"status": "Created", "name": "...", "instanceId": 1234}`

### `unity.gameobject.destroy`
- **Description**: Delete a GameObject.
- **Parameters**:
  - `instanceId` (required, int): InstanceID of the target object.
- **Returns**: Success status.

## Component & Property Management

### `unity.component.add`
- **Description**: Attach a built-in or custom Component to an object.
- **Parameters**:
  - `instanceId` (required, int): Target GameObject.
  - `componentType` (required, string): E.g., `"UnityEngine.Rigidbody"`, `"PlayerController"`.
- **Returns**: Success status.

### `unity.component.property`
- **Description**: Get, Set, or Dump properties of a Component via C# Reflection.
- **Parameters**:
  - `action` (required, string): `"get"`, `"set"`, or `"dump"`.
  - `instanceId` (required, int): Target GameObject.
  - `property` (optional, string): Name of the C# property/field to get/set. Empty string for `dump`.
  - `value` (optional, any): Value to set (number, boolean, string, or vector/color struct).
- **Returns**:
  - `dump`: Array of `"name"`, `"type"`, `"value"`, `"canWrite"`.
  - `get`: Property value dynamically typed.
  - `set`: Success status.

## Scenes

### `unity.scene.manage`
- **Description**: Manage Unity scenes.
- **Parameters**:
  - `action` (required, string): `"open"`, `"save"`, `"new"`, or `"list_build_scenes"`.
  - `path` (optional, string): Path to scene (required for `"open"`).
- **Returns**: Success status or list of scenes.

## Asset Management

### `unity.asset.find`
- **Description**: Search for assets in the project.
- **Parameters**:
  - `filter` (required, string): Unity search filter string (e.g. `"t:Prefab"`, `"Player"`).
  - `folders` (optional, string[]): Array of paths to limit search to.
- **Returns**: `{"assets": ["Assets/Prefabs/Player.prefab", ...]}`

### `unity.asset.create`
- **Description**: Create specific basic assets directly.
- **Parameters**:
  - `action` (required, string): `"folder"` or `"material"`.
  - `path` (required, string): E.g., `"Assets/NewFolder"`.
  - `shader` (optional, string): If material, the shader name (default `"Standard"` or URP equivalent).
- **Returns**: New GUID or Asset Path.

### `unity.asset.meta`
- **Description**: Interrogate and modify Unity `.meta` (Importer) settings directly.
- **Parameters**:
  - `action` (required, string): `"dump"`, `"get"`, or `"set"`.
  - `path` (required, string): Path to the target asset.
  - `property` (optional, string): SerializedProperty path to get/set (e.g., `"m_TextureSettings.m_FilterMode"`).
  - `value` (optional, any): Value to assign on "set".
- **Returns**:
  - `dump`: Array of properties and their data types.
  - `get`: Current value natively extracted.
  - `set`: Success status.

### `unity.asset.refresh`
- **Description**: Manually trigger `AssetDatabase.Refresh()`.
- **Parameters**: None `{}`
- **Returns**: Success status.

### `unity.prefab.instantiate`
- **Description**: Spawn a Prefab asset into the scene.
- **Parameters**:
  - `assetPath` (required, string): E.g., `"Assets/Prefabs/Player.prefab"`.
  - `position` (optional, object): `{"x":0, "y":0, "z":0}`
- **Returns**: Root `instanceId` of instantiated object.

## Build and Testing

### `unity.test.run`
- **Description**: Execute Unity Editor tests.
- **Parameters**:
  - `mode` (required, string): `"editmode"` or `"playmode"`.
- **Returns**: Extensive JSON report including passed/failed counts. Note: Locks the editor during execution.

### `unity.build.manage`
- **Description**: Manage Scripting Defines, or kick off a Build.
- **Parameters**:
  - `action` (required, string): `"get_defines"`, `"set_defines"`, `"build_player"`.
  - `defines` (optional, string): For set_defines, semi-colon separated (e.g., `"DEBUG;PROFILING"`).
  - `buildTarget` (optional, string): For build action, e.g., `"Android"`, `"StandaloneWindows64"`.
- **Returns**: Result of operation or Build Report summary.
