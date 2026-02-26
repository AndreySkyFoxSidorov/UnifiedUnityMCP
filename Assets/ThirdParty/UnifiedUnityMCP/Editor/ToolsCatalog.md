# Unified Unity MCP Tools Catalog

This catalog documents the active tools exposed by the server in [UnityMcpServer.Start()](Assets/ThirdParty/UnifiedUnityMCP/Editor/UnityMcpServer.cs:22).

Protocol baseline for transport + initialize handshake:
- [StreamableHttpTransport](Assets/ThirdParty/UnifiedUnityMCP/Editor/Transport/StreamableHttpTransport.cs:13)
- [InitializeCommand.Execute()](Assets/ThirdParty/UnifiedUnityMCP/Editor/Commands/StandardCommands.cs:12)

## 1) Core Tools (registered directly)

| Tool | Source | Purpose |
|---|---|---|
| `unity_ping` | [UnityPingTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/UnityPingTool.cs:8) | Connectivity/version ping |
| `unity_console_read` | [ConsoleGetLogsTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/ConsoleGetLogsTool.cs:11) | Read Unity Console logs |
| `unity_editor_state` | [EditorGetStateTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/EditorGetStateTool.cs:9) | Get play/pause/compile state |
| `unity_editor_set_state` | [EditorSetStateTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/EditorSetStateTool.cs:9) | Set play/pause/stop |
| `unity_selection_get` | [EditorSelectionTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/EditorSelectionTool.cs:9) | Get current editor selection |
| `unity_gameobject_manage` | [GameObjectManageTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/GameObjectManageTool.cs:12) | `find/create/destroy` scene objects |
| `unity_component_manage` | [ComponentManageTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/ComponentManageTool.cs:10) | `add/remove/list` components |
| `unity_asset_manage` | [AssetManageTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/AssetManageTool.cs:9) | `find/refresh` assets |
| `unity_prefab_instantiate` | [PrefabManageTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/PrefabManageTool.cs:10) | Instantiate prefab |
| `unity_asset_meta` | [AssetMetaManageTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/AssetMetaManageTool.cs:10) | Importer/meta read-write |
| `unity_component_property` | [ComponentPropertyTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/ComponentPropertyTool.cs:14) | Reflection `get/set/dump/invoke` |
| `unity_scene_manage` | [SceneManageTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/SceneManageTool.cs:11) | `open/save/new/list_build_scenes` |
| `unity_asset_create` | [AssetCreateTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/AssetCreateTool.cs:11) | Create material/folder |
| `unity_editor_execute_menu` | [ExecuteMenuTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/ExecuteMenuTool.cs:9) | Execute menu item |
| `unity_test_run` | [TestRunTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/TestRunTool.cs:12) | Run Unity tests |
| `unity_build_manage` | [BuildManageTool](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/BuildManageTool.cs:10) | Defines/build management |

## 2) Skill Module Tools

Factory entrypoint: [UnitySkillModuleTools.CreateAll()](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/UnitySkillModuleTools.cs:151)

### Mapping table: module -> MCP tool

| Skill module | MCP tool name |
|---|---|
| `animator` | `unity_animator` |
| `asset` | `unity_asset` |
| `bookmark` | `unity_bookmark` |
| `camera` | `unity_camera` |
| `cinemachine` | `unity_cinemachine` |
| `cleaner` | `unity_cleaner` |
| `component` | `unity_component` |
| `console` | `unity_console` |
| `debug` | `unity_debug` |
| `editor` | `unity_editor` |
| `event` | `unity_event` |
| `gameobject` | `unity_gameobject` |
| `history` | `unity_history` |
| `importer` | `unity_importer` |
| `light` | `unity_light` |
| `material` | `unity_material` |
| `navmesh` | `unity_navmesh` |
| `optimization` | `unity_optimization` |
| `package` | `unity_package` |
| `perception` | `unity_perception` |
| `physics` | `unity_physics` |
| `prefab` | `unity_prefab` |
| `profiler` | `unity_profiler` |
| `project` | `unity_project` |
| `sample` | `unity_sample` |
| `scene` | `unity_scene` |
| `script` | `unity_script` |
| `scriptableobject` | `unity_scriptableobject` |
| `shader` | `unity_shader` |
| `smart` | `unity_smart` |
| `terrain` | `unity_terrain` |
| `test` | `unity_test` |
| `timeline` | `unity_timeline` |
| `ui` | `unity_ui` |
| `validation` | `unity_validation` |
| `workflow` | `unity_workflow` |

### Module tool behavior

All module tools use [UnityModuleTool.Execute()](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/UnitySkillModuleTools.cs:67):
- `action="list_actions"` (or `help/capabilities`) returns documented actions + implementation flag.
- Implemented actions are dispatched to module handlers.
- Non-implemented documented actions return `status="not_implemented"` with guidance.
- Most modules include `action="bridge"` to forward to existing core tools.

## 3) Coverage status notes

- **Complete module presence**: every module has a corresponding module tool created in [UnitySkillModuleTools.CreateAll()](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/UnitySkillModuleTools.cs:151).
- **Implemented subset**: critical/high-frequency actions (scene/gameobject/component/debug/console/package/physics/validation/project/sample/bookmark/camera/perception/navmesh/history/test) are implemented now.
- **Bridge/not_implemented fallback**: long-tail actions remain documented and callable via module entrypoints, with explicit status feedback.

## 4) Threading and safety

- Module handlers are marshaled onto main thread by [UnityModuleTool.Execute()](Assets/ThirdParty/UnifiedUnityMCP/Editor/Tools/UnitySkillModuleTools.cs:67) via [MainThreadDispatcher.InvokeAsync()](Assets/ThirdParty/UnifiedUnityMCP/Editor/Util/MainThreadDispatcher.cs:45).
- Core tools already marshal Unity API calls internally (or are non-Unity runtime safe where applicable).

## 5) Transport compliance summary

HTTP transport in [StreamableHttpTransport](Assets/ThirdParty/UnifiedUnityMCP/Editor/Transport/StreamableHttpTransport.cs:13) provides:
- Single endpoint path with `POST` + `GET` + optional `DELETE`
- `Accept` / JSON content-type checks
- Session propagation through `Mcp-Session-Id`
- Origin and localhost restrictions

Initialize protocol version: [StandardCommands.InitializeCommand](Assets/ThirdParty/UnifiedUnityMCP/Editor/Commands/StandardCommands.cs:8) now reports `2025-03-26`.
