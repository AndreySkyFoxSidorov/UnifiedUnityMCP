---
name: unity-skills-index
description: "Index of all Unity Skills modules. Use when users want to browse available skills or understand the module structure. Triggers: index, modules, skills, reference, documentation, module, skill list, document."
requires:
  - unified-unity-mcp
---

# Unity Skills - Module Index

> 🛑 **CRITICAL MCP TRANSLATION NOTICE** 🛑
> The sub-modules in this folder were originally written for an older, Python-based `unity_skills` proxy server. They use syntax like `unity_skills.call_skill("gameobject_create", ...)`. 
> **DO NOT USE THAT SYNTAX.**
> You are using the modern, native **Unified Unity MCP Server**. When reading the sub-folders in this index to understand *what* to do, you MUST translate the *how* into the 19 official `unity.*` tools.

## Translation Guide to Unified MCP Tools

When a sub-folder suggests an old command, use this modern equivalent:

| Old `call_skill` Command Family | Modern Unified MCP Tool | Notes |
|---------------------------------|-------------------------|-------|
| `gameobject_create` | `unity.gameobject.create` | Directly creates objects. |
| `gameobject_find` | `unity.gameobject.find` | Returns `instanceId`s. |
| `gameobject_delete` | `unity.gameobject.destroy` | Pass the `instanceId`. |
| `gameobject_set_transform`, `gameobject_set_active`, `gameobject_rename`, `gameobject_set_tag`, `gameobject_set_layer` | `unity.component.property` | Use reflection! Find the `instanceId`, then set properties directly like `name`, `layer`, `tag`, `localPosition`. |
| `component_add`, `component_remove` | `unity.component.add` | For removing, use `unity.gameobject.destroy` on the component's `instanceId`. |
| `component_*` (setting values) | `unity.component.property` | Action="set" for modifying any field. |
| `asset_*`, `material_*`, `importer_*` | `unity.asset.meta` | Dump and Set properties directly on `.meta` files. |
| `scene_*` | `unity.scene.manage` | Open and save scenes natively. |

## Why Read These Sub-Folders Then?

You should read the `SKILL.md` files in the directories below (e.g., `animator`, `cinemachine`, `physics`, `ui`) to learn **what properties to change, best practices, and which components to use** for specific Unity subsystems. Just remember to apply those concepts using `unity.component.property`, `unity.gameobject.create`, etc.

---

## Modules

| Module | Description |
|--------|-------------|
| [gameobject](./gameobject/SKILL.md) | Create, transform, parent GameObjects |
| [component](./component/SKILL.md) | Add, remove, configure components |
| [material](./material/SKILL.md) | Materials, colors, emission, textures |
| [light](./light/SKILL.md) | Lighting setup and configuration |
| [prefab](./prefab/SKILL.md) | Prefab creation and instantiation |
| [asset](./asset/SKILL.md) | Asset import, organize, search |
| [ui](./ui/SKILL.md) | Canvas and UI element creation |
| [script](./script/SKILL.md) | C# script creation and search |
| [scene](./scene/SKILL.md) | Scene loading, saving, hierarchy |
| [editor](./editor/SKILL.md) | Play mode, selection, undo/redo |
| [animator](./animator/SKILL.md) | Animation controllers and parameters |
| [shader](./shader/SKILL.md) | Shader creation and listing |
| [console](./console/SKILL.md) | Log capture and debugging |
| [validation](./validation/SKILL.md) | Project validation and cleanup |
| [importer](./importer/SKILL.md) | Texture/Audio/Model import settings |
| [cinemachine](./cinemachine/SKILL.md) | Virtual cameras and cinematics |
| [terrain](./terrain/SKILL.md) | Terrain creation and painting |
| [physics](./physics/SKILL.md) | Raycasts, overlaps, gravity |
| [navmesh](./navmesh/SKILL.md) | Navigation mesh baking |
| [timeline](./timeline/SKILL.md) | Timeline and cutscenes |
| [workflow](./workflow/SKILL.md) | Undo history and snapshots |
| [cleaner](./cleaner/SKILL.md) | Find unused/duplicate assets |
| [smart](./smart/SKILL.md) | Query, layout, auto-bind |
| [perception](./perception/SKILL.md) | Scene analysis and summary |
| [camera](./camera/SKILL.md) | Scene View camera control |
| [event](./event/SKILL.md) | UnityEvent listeners |
| [package](./package/SKILL.md) | Package Manager operations |
| [project](./project/SKILL.md) | Project info and settings |
| [profiler](./profiler/SKILL.md) | Performance statistics |
| [optimization](./optimization/SKILL.md) | Asset optimization |
| [sample](./sample/SKILL.md) | Basic test skills |
| [debug](./debug/SKILL.md) | Error checking and diagnostics |
| [test](./test/SKILL.md) | Unity Test Runner |
| [bookmark](./bookmark/SKILL.md) | Scene View bookmarks |
| [history](./history/SKILL.md) | Undo/redo history |
| [scriptableobject](./scriptableobject/SKILL.md) | ScriptableObject management |
