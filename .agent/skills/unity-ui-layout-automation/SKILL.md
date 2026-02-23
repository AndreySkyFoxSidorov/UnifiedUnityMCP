---
name: unity-ui-layout-automation
description: Automates and validates Unity UI (uGUI) layout structures based on established project patterns. Use when creating new UI screens, porting UI approaches between projects, or validating UI hierarchies without relying on specific prefab names. Ensures proper Canvas setup, animation behaviors, localization components, and responsive groupings.
---

# Unity UI Layout Automation & Validation

This skill provides generic, robust rules for laying out and validating UI windows/screens in Unity. It abstracts the approaches used in standard project UI, ensuring that new or ported UI conforms to structural best practices without depending on hardcoded prefab or window names.

## Principles of the UI Architecture

1. **Self-Contained Canvas Roots**: Every primary screen or popup should be a self-contained prefab with its own `Canvas`, `CanvasScaler`, and `GraphicRaycaster`.
2. **Animation Overlays**: The root and its major child panels must support unified animation/transitions via the `UIAnimationBehaviour` component.
3. **Localization Enforcement**: Any `Text` component must be accompanied by a `LocalizeUIText` component to ensure global string resolution.
4. **Responsive Containers**: Layout groups and `AspectRatioFitter` components should be used to maintain consistency across resolutions.
5. **Particle-UI Integration**: Particle Systems inside UI must utilize `Coffee.UIExtensions.UIParticle` to render in Canvas space correctly.

## Validation Checklist (Name-Agnostic)

When building or reviewing a UI screen, programmatically or manually validate the following hierarchy invariants:

### 1. Root Element Validation
The absolute root GameObject of the window/screen prefab MUST contain the following components:
- `UnityEngine.RectTransform`
- `UnityEngine.Canvas`
- `UnityEngine.UI.CanvasScaler`
- `UnityEngine.UI.GraphicRaycaster`
- `UIAnimationBehaviour` (for structural transitions)
- The main logical controller script (e.g., ending in `Screen`, `Popup`, or `Window`). *Do not validate the exact name, only its presence.*

### 2. Panel Organization
- The root should contain 1 or more logical "Panels" as children (e.g., an overlay container, top bar, bottom bar).
- If a panel fades or animates independently, it should have a `UnityEngine.CanvasGroup` and its own `UIAnimationBehaviour`.

### 3. Localization Component Verification
**Rule:** No bare texts.
- Find all GameObjects containing `UnityEngine.UI.Text` (or `TextMeshProUGUI` if upgraded).
- For each Text GameObject, verify that `LocalizeUIText` is also attached.
- *Fix Action:* If missing, automatically attach `LocalizeUIText`.

### 4. Interactive Elements Breakdown
- `UnityEngine.UI.Button` components should typically be on the same GameObject as their target `UnityEngine.UI.Image`.
- If the button has text, it must be a child GameObject (e.g., "Text (Legacy)"), obeying the localization rule.

### 5. Canvas Particles
- Any `UnityEngine.ParticleSystem` within the hierarchy must have a sibling `Coffee.UIExtensions.UIParticle` component.

## Usage: Applying the Validation via MCP

When transferring these approaches to a new project or auto-validating, use the `unity-mcp-orchestrator` tools.

**Example: Validating a Screen Hierarchy**
1. Get the hierarchy of the target prefab using:
   `manage_prefabs(action="get_hierarchy", prefab_path="Assets/Path/To/AnyScreen.prefab")`
2. Walk the returned `items` JSON array.
3. Identify the `isRoot: true` element. Verify `componentTypes` contains `Canvas`, `CanvasScaler`, `GraphicRaycaster`, and `UIAnimationBehaviour`.
4. Filter all items where `componentTypes` includes `UnityEngine.UI.Text`. Verify that each of these items also includes `LocalizeUIText` in its `componentTypes`.
5. If validations fail, use `unity.component.add` or `unity.prefab.instantiate` to inject the missing components (e.g., adding `LocalizeUIText` or `UIAnimationBehaviour`).

## Usage: Creating a New Screen

When instructed to create a new UI element:
1. Initialize the Root GameObject with the standard Canvas stack and `UIAnimationBehaviour`.
2. Group logical sections into empty `RectTransform` parents (CanvasGroup optional).
3. Populate children with standard Unity UI Elements, ensuring to add `LocalizeUIText` on all text nodes.
4. Scale using `AspectRatioFitter` and standard UI layout groups.
