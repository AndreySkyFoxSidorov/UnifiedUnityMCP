# Tools Catalog

## 1. CoplayDev-unity-mcp
*This server uses string-based identifiers and loosely typed JObject payloads for arguments. Many tools act as generic dispatchers for multiple actions (e.g. `action: "create"`, `action: "delete"`).*

| Tool/Method | Purpose | Params schema | Result schema | Unity main-thread required | Notes |
|-------------|---------|---------------|---------------|----------------------------|-------|
| `execute_menu_item` | Execute an Editor Menu Item | `{ "menuPath": string }` | Text/Success | Yes | |
| `find_gameobjects` | Find objects in scene by name/tag/layer | `{ "name": string, "tag": string, "layer": string }` | List of references | Yes | |
| `manage_script` | Attach/detach scripts | `{ "action": string, "target": string, "scriptName": string }` | Success/Failure | Yes | Includes actions like `attach`, `detach`. |
| `refresh_unity` | Refresh AssetDatabase | `{}` | Success | Yes | `AssetDatabase.Refresh()` |
| `run_tests` | Run EditMode/PlayMode tests | `{ "mode": string }` | Job ID string | Yes | Runs synchronously or async depending on mode. |
| `manage_prefabs` | Open/Close prefab stages | `{ "action": string, "path": string }` | Success/Failure | Yes | Actions: `open_stage`, `close_stage`, `save`. |
| `read_console` | Capture Unity Editor Console | `{ "logType": string, "maxLines": int }` | String of logs | Yes | Reads `LogEntries` via reflection. |
| `manage_gameobject` | CRUD for GameObjects | `{ "action": string, "target": string, "componentProperties": object }` | ID / Success | Yes | Heavy dispatcher: `create`, `modify`, `delete`, `duplicate`. |
| `manage_asset` | CRUD for Assets | `{ "action": string, "path": string, "newPath": string }` | Success/Failure | Yes | Dispatcher: `copy`, `move`, `delete`, `create_folder`. |
| `manage_editor` | Play/Pause/Stop/Select | `{ "action": string, "target": string }` | State/Success | Yes | `play`, `pause`, `stop`, `select`. |

*Note: This server also supports `initialize`, `tools/list`, and `tools/call` JSON-RPC methods.*

---

## 2. IvanMurzak-Unity-MCP
*This server uses strongly-typed attributes `[McpPluginTool]` with Reflection. Tools are granular and map closely to single C# methods.*

| Tool/Method | Purpose | Params schema | Result schema | Unity main-thread required | Notes |
|-------------|---------|---------------|---------------|----------------------------|-------|
| `Assets.Find` | Find asset GUIDs | `{ "filter": string, "searchInFolders": string[] }` | List of GUIDs/Paths | Yes | Uses `AssetDatabase.FindAssets` |
| `Assets.CreateFolders` | Create missing folders | `{ "parentFolder": string, "newFolderName": string }` | Success/Path | Yes | |
| `GameObject.Create` | Instantiate new GameObject | `{ "name": string, "parentGameObjectRef": object, "position": vector3 }` | `GameObjectRef` | Yes | Highly typed parameters (Vector3, bool). |
| `GameObject.Destroy` | Destroy a GameObject | `{ "gameObjectRef": object }` | Success | Yes | |
| `GameObject.Find` | Find by Name/Tag | `{ "name": string, "tag": string }` | List of `GameObjectRef` | Yes | |
| `GameObject.Component.Add` | Add a component | `{ "gameObjectRef": object, "componentType": string }` | `ComponentRef` | Yes | |
| `Editor.Application.GetState` | IsPlaying, IsPaused | `{}` | `{ isPlaying: bool, isPaused: bool }` | Yes | |
| `Editor.Selection.Get` | Get selected objects | `{}` | List of `GameObjectRef` | Yes | |

*Note: This server also implements strict JSON schemas derived from C# parameter types via reflection.*

---

## 3. Combined Targets for Unified Server
*We will implement a clean, granular set of tools blending the best of both approaches. Tools will have distinct names (avoiding generic `manage_xxx` dispatchers).*

| Tool/Method | Purpose | Params schema | Result schema | Unity main-thread required |
|-------------|---------|---------------|---------------|----------------------------|
| `unity_ping` | Basic connectivity check & version | `{}` | `{ version: string }` | No (or Yes for version) |
| `unity_console_read` | Read Editor Console logs | `{ "maxLines": int, "typeFilter": string }` | Array of log strings | Yes |
| `unity_editor_state` | Get/Set PlayMode state | `{ "state": string ("play","pause","stop") }` | `{ isPlaying: true }` | Yes |
| `unity_selection_get` | Get currently selected objects | `{}` | Array of object paths/IDs | Yes |
| `unity.selection.set` | Set selection | `{ "instanceIds": int[] }` | Success | Yes |
| `unity.gameobject.find` | Find objects by name or tag | `{ "name": string, "tag": string }` | Array of object info | Yes |
| `unity.gameobject.create`| Create empty or primitive | `{ "name": string, "primitiveType": string, "parentRoute": string }`| New Object ID | Yes |
| `unity.gameobject.destroy`| Destroy an object | `{ "instanceId": int }` | Success | Yes |
| `unity.component.add` | Add component to object | `{ "instanceId": int, "componentType": string }` | Success | Yes |
| `unity.asset.find` | Find assets | `{ "filter": string, "folders": string[] }` | Array of paths | Yes |
| `unity.asset.refresh` | Refresh AssetDatabase | `{}` | Success | Yes |
| `unity_asset_meta` | Dump/Get/Set .meta properties via Importer | `{ "action": string, "path": string, "property": string, "value": any }` | Success/List | Yes |
| `unity_prefab_instantiate`| Instantiate prefab at path | `{ "assetPath": string, "position": object }` | New Object ID | Yes |
| `unity_component_property`| Get/Set component properties via reflection | `{ "action": string, "instanceId": int, "property": string, "value": any }` | Success/Value | Yes |
| `unity_scene_manage`      | Open/Save/Create scenes  | `{ "action": string, "path": string }`        | Success/State   | Yes |
| `unity_asset_create`      | Create Materials/Folders | `{ "action": string, "path": string, "shader": string }` | Success/Path  | Yes |
| `unity_test_run`          | Run Unity Edit/Play tests| `{ "mode": "editmode" or "playmode" }`        | Test Results    | Yes |
| `unity_editor_execute_menu`| Execute top menu items  | `{ "menuPath": string }`                      | Success         | Yes |
| `unity_build_manage`      | Get/Set defines, Build   | `{ "action": string, "defines": string, "buildTarget": string }` | Success/Report | Yes |
