---
name: unity-ui
description: Unity UI guidance for UGUI and UI Toolkit. Use when creating, reviewing, or optimizing UI in a target Unity project through MCP tools or source edits.
---

# Unity UI

## Use This Skill When

- Creating or modifying UGUI or UI Toolkit.
- Reviewing Canvas hierarchy, event system, layout groups, anchors, or UI performance.
- Using MCP UI tools in a target project.

## Rules

- Check whether the target project uses UGUI, UI Toolkit, or both.
- Match the existing UI system and hierarchy.
- Keep layout explicit and inspectable.
- Avoid nested layout groups and `ContentSizeFitter` chains unless needed.
- Split canvases by update frequency when performance requires it.
- Do not add UI packages or frameworks without explicit need.
- Do not use `switch`.

## Verification

- Inspect hierarchy after changes.
- Check anchors, scaling, and safe-area behavior when relevant.
- Check console errors.
- For user-facing UI, verify at representative aspect ratios.
