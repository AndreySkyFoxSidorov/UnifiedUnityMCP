---
name: unity-mobile
description: Mobile-specific Unity review guidance. Use only when the target Unity project or MCP task explicitly concerns Android, iOS, WebGL-on-mobile, mobile performance, or mobile asset settings.
---

# Unity Mobile

## Use This Skill When

- Reviewing Android or iOS project settings.
- Checking mobile asset import settings, texture formats, UI scale, or frame budget.
- Preparing a target Unity project for mobile deployment through MCP tools.

## Rules

- Do not apply mobile constraints to the MCP server itself unless the task is about mobile support.
- Check the target platform and Unity version before changing settings.
- Prefer targeted importer or build setting changes over broad project rewrites.
- Keep mobile recommendations optional unless required by the task.
- Do not add packages or platform plugins without explicit need.
- Do not use `switch`.

## Typical Checks

- Texture compression matches target platform.
- UI layout works on aspect ratio and safe-area constraints.
- Build target, scripting backend, and stripping settings are intentional.
- Console has no repeated errors or log spam.
