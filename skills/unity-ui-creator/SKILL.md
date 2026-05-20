---
name: unity-ui-creator
description: Create practical UGUI screen, menu, HUD, popup, and panel hierarchies. Use when building UI objects or prefabs in a Unity project.
---

# Unity UI Creator

## Use This Skill When

- Creating a UGUI screen, menu, HUD, popup, or settings panel.
- Building a Canvas hierarchy through MCP tools.
- Adding buttons, toggles, sliders, input fields, panels, images, or TextMeshPro labels.

## Minimum Inputs

- Target UI system: UGUI or UI Toolkit.
- Target scene or prefab path.
- Screen purpose and required controls.
- Resolution or aspect ratio constraints when known.

## UGUI Structure

Use a direct hierarchy:

```text
Canvas
EventSystem
ScreenRoot
  Background
  Content
  Header
  Footer
  ModalLayer
```

Adjust names to the project. Do not force this exact naming when an existing convention is present.

## Rules

- Prefer anchors and layout groups over hard-coded pixel positions.
- Keep interactive elements easy to find.
- Use TextMeshPro only when the package exists or the project already uses it.
- Keep animation helpers optional and project-local.
- Do not add scripts from this skill unless the user asks for runtime behavior.
- Do not use `switch`.

## Delivery Checklist

- Hierarchy is clean.
- Controls are named clearly.
- Canvas scaler and anchors are intentional.
- Console has no UI errors.
- Layout works in the requested aspect ratios.
