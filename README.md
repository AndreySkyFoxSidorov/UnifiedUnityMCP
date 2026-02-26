# 🚀 Knowledge & Skills Base (Skills & MCP)
Global infrastructure for automating the Unity Editor using Artificial Intelligence (Antigravity & Unified MCP)

## 🔌 Unified Unity MCP Server
Our optimized Model Context Protocol server that provides direct, secure, and strictly typed control over Unity objects from the AI chat.

- **`unity.gameobject.*`**: Search (including returning `instanceId`), creation (primitives, empty objects), and deletion of GameObject hierarchies.
- **`unity.component.*`**: Higher level control: adding components and manipulating properties through deep C# reflection (changing coordinates, colliders, colors, scripts).
- **`unity.asset.*`**: Direct modification of `.meta` files (e.g., texture compression, image filters) without parsing YAML, as well as searching the AssetDatabase.
- **`unity.scene.*` / `unity.prefab.*`**: Saving/opening scenes in the Editor. Instantiating existing prefabs into the active scene.
- **`unity.editor.*`**: Checking Editor status (whether compilation is ongoing), executing menu items (Execute Menu), and changing active selection (Selection).
- **Other (Tests, Logs, Builds)**: Running `unity.test.run`, injecting `#define` symbols via `unity.build.manage`, subscribing to `Debug.Log`.

---

## 🧠 Core Developer Skills
Main instruction skills (Prompts & Skills) that teach AI the best architectural practices for development in Unity and C#.

- **`csharp-pro` (C# 9.0+)**: Teaches AI modern C#: using records, pattern matching, memory optimization (span, value types), and SOLID principles.
- **`csharp-async-patterns` (Async/Await)**: Strict asynchronous standards: proper cancellation via `CancellationToken`, using `ConfigureAwait`, avoiding `async void`.
- **`unity-developer` (Architecture)**: Development lead. Component-based approach, caching via `TryGetComponent`, using Addressables, ScriptableObjects, and State Machine patterns.
- **`unity-async` (Unity Threading)**: Threading specifics in Unity. Delegating coroutines (legacy) and Tasks (modern), ensuring Unity API calls strictly on the Main Thread.
- **`unity-performance & mobile` (Optimization)**: FPS optimization for smartphones. Using IL2CPP, layout batching (Draw Calls & SRP Batcher), Occlusion Culling, and texture compression (ASTC/ETC2).
- **`unity-collection-pool` (Zero-Allocation)**: A skill that trains AI to completely avoid Garbage Collection spikes by using `ListPool` and `DictionaryPool` from `UnityEngine.Pool`.

---

## 🎨 UI Skills (UI & UI Automation)

- **`unity-ui-creator` (GUI Layout)**: The absolute expert in Screen/HUD/Menu layout in Unity. Teaches to create the correct Canvas hierarchy with Anchors, adapt to different screens (Landscape/Portrait, notches), and avoid Layout "flickering".
- **`unity-ui-layout-automation` (Auto-validation)**: Scripts and instructions for automatic validation of Canvas structure, adding text localization, and checking for animations via MCP.
- **`unity-textmeshpro` (Typography)**: Deep knowledge of TMPro profiling: creating Font Assets, Rich Text, materials for shadows/highlights.
- **`csharp-xml-docs` (Documentation)**: Strict standard for automatic XML documentation: creating clear `<summary>`, `<param>`, `<returns>` in pure English without fluff (Haiku Style).

---

## 🛠 The Grand Unity Hierarchy
A giant educational reference manual divided into 35+ micro-skills. **Important:** The AI uses these knowledges as a *"What to do" reference*, but it automatically *translates* commands via the Unified Unity MCP Server using a special mapping table.

| Sub-Skill | Description (Area of Responsibility) |
| :--- | :--- |
| `gameobject`, `component`, `prefab` | Main object manipulations: hierarchical creation, script attachment, and prefab spawning. |
| `scene`, `asset`, `importer` | Loading scenes, moving objects via directories in the AssetDatabase, configuring texture/audio/model compression. |
| `material`, `shader`, `light` | Setting colors/textures in materials, creating custom shaders, configuring global lighting. |
| `physics`, `navmesh`, `terrain` | Executing Raycasts, configuring colliders and gravity, baking pathways for artificial intelligence, sculpting forests/mountains on Terrain. |
| `editor`, `camera`, `console` | Managing the Editor itself: Play Mode, selection, moving the camera in the Scene view, searching for errors in the console. |
| `cinemachine`, `timeline`, `animator` | Configuring virtual and follow cameras (Follow/LookAt), generating cutscenes, creating animation controllers. |
| `ui`, `event`, `scriptableobject` | Building buttons, configuring onClick() events for UI, generating and editing ScriptableObject (as game config database). |
| `optimization`, `profiler`, `validation`| Checking the project for "broken" scripts and missing references, collecting performance stats. |
| `smart`, `cleaner`, `perception` | Smart SQL queries for objects inside the scene (search by color or layer), removing dead or duplicated code, analyzing scene dependencies. |

---

## 💡 Effective Prompts Examples (English Prompts)
Below are examples of perfect queries (prompts). They allow the AI to use the embedded skills and Unified MCP server as efficiently as possible.

### 🖥️ UI and Layout Skills (`unity-ui-creator`, `textmeshpro`)
- `Use the 'unity-ui-creator' skill. Build a highly responsive "Settings Menu" screen in the UI scene. It should have a background panel, vertical layout group in the center, 3 buttons (Audio, Graphics, Gameplay), and a close button anchored to the top right. Test it on 16:9 and notch displays.`
- `I need a ScrollView for an inventory system. Please structure the prefabs correctly, removing any unnecessary ContentSizeFitters to ensure 60fps scrolling.`
- `Use 'unity-textmeshpro' to create a stylized "Game Over" title text. Apply a gold metallic material preset with a subtle drop shadow and ensure the font asset supports rich text formatting.`
- `Validate my HUD canvas hierarchy using the 'unity-ui-layout-automation' checks. injected the missing UIAnimationBehaviour to the root if needed.`

### 🧩 Architecture and C# (`unity-developer`, `csharp-pro`)
- `Act as a 'unity-developer'. I need to implement a state machine for my boss enemy. Use modern C# 9.0 features like pattern matching and switch expressions to handle phase transitions.`
- `Refactor my DataService layer using 'csharp-async-patterns'. Remove all 'async void' methods except for UI click handlers, and ensure every web request properly accepts a CancellationToken.`
- `Use 'unity-collection-pool' to rewrite my projectile spawner. Currently, it allocates a new List every frame. Replace it with ListPool from UnityEngine.Pool to eliminate Garbage Collection spikes completely.`
- `Apply 'csharp-code-style' to the entirely of the CombatManager.cs script. Fix all naming conventions so that private fields use the 'm' prefix and booleans use the 'b' prefix.`
- `Generate XML documentation for the IPlayerInventory interface using the 'csharp-xml-docs' Haiku style. Keep it concise, remove all redundant words, and use active voice.`

### 🔋 Optimization and Mobile (`unity-performance`, `unity-mobile`)
- `Act as a 'unity-performance' expert. Use the MCP to check the materials on all Environment meshes and turn on GPU Instancing for all of them using reflection.`
- `Review my project for mobile deployment using 'unity-mobile' standards. Check the Texture Importer settings using 'unity.asset.meta' and ensure large UI sprites are using ASTC 6x6 compression.`
- `Use the MCP 'unity.console.read' tool to monitor my scene for 10 seconds. Identify if any scripts are wildly spamming Debug.Log and causing CPU bottlenecks.`
- `I have 30 identical trees in the scene. Use the MCP tools to find them all by their 'Tree' tag, and parent them under a single empty 'Environment' GameObject to clean up the hierarchy.`

### 🛠️ Scene and Object Manipulations
- `Use the 'unity.gameobject.find' tool to find the GameObject named 'PlayerSpotlight'. Then use 'unity.component.property' to dynamically change its Light intensity to 12.5 and its color to pure Red.`
- `I need to set up a Cinemachine Virtual Camera focusing on the Player. Instantiate the VCam prefab, then use reflection to set its Follow and LookAt targets to the Player's instanceId.`
- `Check the 'unity.editor.state'. If it is not compiling, trigger a full 'unity.test.run' on PlayMode. Output the results of any failed tests.`
- `Use the 'unity.asset.create' tool to scaffold 3 new empty folders in my project: Assets/Art/Characters, Assets/Audio/SFX, and Assets/Data/Configs.`
- `Find all GameObjects with the 'BoxCollider' component attached. For each one, use 'unity.component.property' to set their 'isTrigger' boolean to true.`
- `Use the 'unity_navmesh' MCP tool to bake a new navigation mesh for the current scene geometry.`
- `Save my current scene using 'unity.scene.manage', then open 'Assets/Scenes/Level_02.unity'. Once opened, find the 'SpawnPoint' object and move it to coordinates 10, 0, 50.`

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

