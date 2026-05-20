---
name: unity-ui-layout-automation
description: Validate and automate Unity UGUI layout structure. Use when checking UI hierarchy quality, responsive grouping, canvas setup, and repeated screen patterns.
---

# Unity UI Layout Automation

## Use This Skill When

- Validating an existing UGUI hierarchy.
- Porting a UI layout pattern between Unity projects.
- Creating repeated screen or popup structures through MCP tools.

## Validation Checklist

- Root canvas and event system are present when needed.
- Canvas scaler matches the target project.
- Root anchors and stretch settings support the requested aspect ratios.
- Layout groups are not nested deeper than necessary.
- Scroll views have stable viewport, content, and item structure.
- Interactive controls are grouped by purpose.
- Text uses the project's current text system.

## Rules

- Do not depend on exact object names unless the target project already requires them.
- Prefer name-agnostic validation based on components and hierarchy shape.
- Do not add animation, localization, particles, or custom helper scripts unless the target project already uses them or the user asks.
- Do not use `switch`.

## MCP Workflow

- Inspect scene or prefab hierarchy first.
- Apply the smallest structural fix.
- Re-read hierarchy and console output.
- Report remaining layout risks.
