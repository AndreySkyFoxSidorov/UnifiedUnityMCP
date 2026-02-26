# 🚀 Knowledge & Skills Base (Skills & MCP)
Global infrastructure for automating the Unity Editor using Artificial Intelligence (Antigravity & Unified MCP)

## 🔌 Unified Unity MCP Server
Our optimized Model Context Protocol server that provides direct, secure, and strictly typed control over Unity objects from the AI chat.

### ⚙️ Antigravity Configuration 
To connect your AI chat client to the running Unity Editor, add the following configuration:
```json
{
  "mcpServers": {
    "unityMCP": {
      "type": "sse",
      "serverUrl": "http://127.0.0.1:18008/mcp",
      "disabled": false,
      "alwaysAllow": [],
      "disabledTools": []
    }
  }
}
```

The server provides **52+ tools** covering almost every aspect of Unity:
- **`unity_gameobject`**, **`unity_component`**, **`unity_prefab`**: Creation, search (returns `instanceId`), and deletion of GameObject hierarchies, including adding/removing components and instantiating prefabs.
- **`unity_component_property`**, **`unity_component_manage`**: Advanced control for getting/setting properties on components via deep C# reflection and invoking component methods.
- **`unity_asset`**, **`unity_asset_meta`**, **`unity_importer`**: File moving, finding, and direct modification of `.meta` properties (e.g., texture compression, image filters) via SerializedProperty paths without parsing YAML.
- **`unity_scene`**, **`unity_scene_manage`**: Saving, opening, creating scenes, and returning build settings scenes.
- **`unity_editor`**, **`unity_editor_state`**, **`unity_editor_execute_menu`**: Checking Editor status (Play, Pause, Compiling), executing Editor menu items, and handling history via **`unity_history`**.
- **`unity_build_manage`**, **`unity_test_run`**, **`unity_package`**: CI/CD tools to get/set Scripting Defines, trigger builds, check packages, and run EditMode/PlayMode tests.
- **`unity_console`**, **`unity_debug`**, **`unity_profiler`**: Reading console logs efficiently, debugging diagnostics, and capturing performance statistics.
- **`unity_ui`**, **`unity_camera`**, **`unity_cinemachine`**: Unified UI creation (uGUI), SceneView camera controls, and Cinemachine orchestration.
- **`unity_physics`**, **`unity_navmesh`**, **`unity_terrain`**: Advanced scene tools for colliders, baked navmesh pathways, and terrain sculpting.
- **`unity_material`**, **`unity_shader`**, **`unity_light`**: Controlling rendering pipelines, colors/textures on materials, shaders, and global lighting tuning.
- **`unity_animator`**, **`unity_timeline`**: Workflows and controller operations for Unity's Animator, and authoring Timeline sequences.
- **`unity_script`**, **`unity_scriptableobject`**, **`unity_event`**: Managing C# scripts, editing ScriptableObject assets, and modifying UnityEvents safely.
- **`unity_cleaner`**, **`unity_validation`**, **`unity_optimization`**: Project cleanup, running project validations, removing dead code, and optimizing scenes.
- **`unity_smart`**, **`unity_perception`**, **`unity_workflow`**: Smart queries layout binding, deep introspection/reporting of scene state, and managing workflow histories.

---

## 🧠 Core Developer Skills
Main instruction skills that teach Antigravity the best architectural practices for development in Unity and C#.

### 🏗️ Architecture & C# Standards
- **`csharp-pro`**: Teaches modern C# (9.0+) including records, pattern matching, memory optimization (span, value types), and SOLID principles.
- **`csharp-async-patterns`**: Strict asynchronous standards: proper cancellation via `CancellationToken`, using `ConfigureAwait`, avoiding `async void`.
- **`csharp-code-style`**: Code style and naming conventions (mPascalCase for private, bBoolean prefix), project code organization, and reviewing standard consistency.
- **`unity-csharp-fundamentals`**: Unity C# fundamental patterns including `TryGetComponent`, `SerializeField`, `RequireComponent`, and safe coding practices.
- **`unity-developer`**: Development lead. Component-based approach, caching dependencies, Addressables, ScriptableObjects, and State Machine patterns.
- **`unity-async`**: Threading specifics in Unity. Delegating coroutines (legacy) and Tasks (modern), ensuring Unity API calls execute strictly on the Main Thread.
- **`unity-collection-pool`**: Trains the AI to completely avoid Garbage Collection spikes by using `ListPool` and `DictionaryPool` from `UnityEngine.Pool`.

### 🚀 Optimization, Testing & Docs
- **`unity-performance`**: Optimization through profiling, draw call reduction, batching, LOD, and occlusion culling.
- **`unity-mobile`**: FPS optimization for smartphones, using IL2CPP, layout batching, and texture compression (ASTC/ETC2).
- **`unity-testrunner`**: Unity Test Framework CLI automation, EditMode/PlayMode testing, NUnit assertions, and Test-Driven Development (TDD) workflows.
- **`csharp-xml-docs`**: Strict standard for automatic XML documentation: creating clear `<summary>`, `<param>`, `<returns>` in concise English.

---

## 🎨 UI & Presentation Skills

- **`unity-ui`**: Build and optimize Unity UI with UI Toolkit and UGUI. Responsive layouts, event systems, and layout optimization.
- **`unity-ui-creator`**: The absolute expert in Screen/HUD/Menu layout in Unity. Teaches to create the correct Canvas hierarchy with Anchors, adapt to different screens, and avoid Layout "flickering".
- **`unity-ui-layout-automation`**: Automates and validates Unity UI (uGUI) layout structures based on established project patterns.
- **`unity-textmeshpro`**: Deep knowledge of TMPro: creating Font Assets, Rich Text, materials for shadows/highlights, and dynamic performance.

---

## ⚙️ Meta & System Skills
Skills used for managing and developing the AI systems and server infrastructure.

- **`unified-unity-mcp`**: Information on operating Unity Editor through the Unified Unity MCP core + its module tools.
- **`skill-creator`**: Guide for creating effective skills. Teaches the AI how to properly formulate, format, and structure new instruction modules.

---

## 💡 Effective Prompts Examples
Below are examples of perfect queries (prompts). They instruct Antigravity to use the embedded skills and Unified MCP server as efficiently as possible.

### 🖥️ UI and Layout Skills
- `Use the 'unity-ui-creator' skill. Build a highly responsive "Settings Menu" screen in the UI scene. It should have a background panel, vertical layout group in the center, 3 buttons (Audio, Graphics, Gameplay), and a close button anchored to the top right. Test it on 16:9 and notch displays.`
- `I need a ScrollView for an inventory system. Please structure the prefabs correctly, removing any unnecessary ContentSizeFitters to ensure 60fps scrolling.`
- `Use the 'unity-textmeshpro' skill to create a stylized "Game Over" title text. Apply a gold metallic material preset with a subtle drop shadow and ensure the font asset supports rich text formatting.`
- `Validate my HUD canvas hierarchy using the 'unity-ui-layout-automation' checks. Inject the missing UIAnimationBehaviour to the root if needed.`

### 🧩 Architecture and C#
- `Use the 'unity-developer' skill. I need to implement a state machine for my boss enemy. Use modern C# 9.0 features like pattern matching and switch expressions to handle phase transitions.`
- `Refactor my DataService layer using the 'csharp-async-patterns' skill. Remove all 'async void' methods except for UI click handlers, and ensure every web request properly accepts a CancellationToken.`
- `Use the 'unity-collection-pool' skill to rewrite my projectile spawner. Replace standard lists with ListPool from UnityEngine.Pool to eliminate GC spikes.`
- `Apply the 'csharp-code-style' skill to the entirety of the CombatManager.cs script. Fix all naming conventions so that private fields use the 'm' prefix and booleans use the 'b' prefix.`
- `Generate XML documentation for the IPlayerInventory interface using the 'csharp-xml-docs' skill. Keep it concise and use active voice.`

### 🔋 Optimization and Mobile
- `Use the 'unity-performance' skill. Use the MCP to check the materials on all Environment meshes and turn on GPU Instancing for all of them using reflection.`
- `Review my project for mobile deployment using the 'unity-mobile' skill. Check the Texture Importer settings using 'unity_asset_meta' and ensure large UI sprites are using ASTC 6x6 compression.`
- `Use the MCP 'unity_console_read' tool to monitor my scene for 10 seconds. Identify if any scripts are wildly spamming Debug.Log and causing CPU bottlenecks.`
- `I have 30 identical trees in the scene. Use the MCP tools to find them all by their 'Tree' tag, and parent them under a single empty 'Environment' GameObject to clean up the hierarchy.`

### 🛠️ Scene and Object Manipulations
- `Use the 'unity_gameobject_manage' tool to find the GameObject named 'PlayerSpotlight'. Then use 'unity_component_property' to dynamically change its Light intensity to 12.5 and its color to pure Red.`
- `I need to set up a Cinemachine Virtual Camera focusing on the Player. Instantiate the VCam prefab, then use reflection to set its Follow and LookAt targets to the Player's instanceId.`
- `Check the 'unity_editor_state'. If it is not compiling, trigger a full 'unity_test_run' on PlayMode. Output the results of any failed tests.`
- `Use the 'unity_asset_create' tool to scaffold 3 new empty folders in my project: Assets/Art/Characters, Assets/Audio/SFX, and Assets/Data/Configs.`
- `Find all GameObjects with the 'BoxCollider' component attached. For each one, use 'unity_component_property' to set their 'isTrigger' boolean to true.`
- `Use the 'unity_navmesh' MCP tool to bake a new navigation mesh for the current scene geometry.`
- `Save my current scene using 'unity_scene_manage', then open 'Assets/Scenes/Level_02.unity'. Once opened, find the 'SpawnPoint' object and move it to coordinates 10, 0, 50.`

---

P.S. By the way, here’s the extensions pack I use in Antigravity to work comfortably with Unity + C#:

C# (OmniSharp): IntelliSense, navigation, refactoring, basic debugging
https://open-vsx.org/extension/muhammad-sammy/csharp

.NET Runtime tool: automatically pulls the required .NET runtime for C# tooling
https://open-vsx.org/extension/ms-dotnettools/vscode-dotnet-runtime

Unity attach debugger / integration: launch & attach to Unity Editor/Player
https://open-vsx.org/extension/Zlorn/vstuc

Solution Explorer: convenient navigation for .sln / .csproj
https://open-vsx.org/extension/fernandoescolar/vscode-solution-explorer

Unity Tools pack: small but useful Unity-focused helpers
https://open-vsx.org/extension/Tobiah/unity-tools

Shader support: HLSL / Unity shader tooling via language server
https://open-vsx.org/extension/shader-ls/vscode-shader

Unity Color Preview: inline preview for Color/Color32
https://open-vsx.org/extension/clock-worked/vscode-unity-color-preview

Unity C# snippets: quick templates for common Unity code
https://open-vsx.org/extension/kleber-swf/unity-code-snippets
