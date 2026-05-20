---
name: unity-developer
description: Implement Unity features in the Unified Unity MCP package style. Use for Unity Editor server work, Unity tooling, gameplay-facing examples, and package architecture decisions.
---

# Unity Developer

## Use This Skill When

- Changing Unity Editor tools, commands, tests, or package docs.
- Adding a new MCP tool or extending an existing one.
- Reviewing Unity architecture inside `Assets/ThirdParty/UnifiedUnityMCP`.

## Architecture

- Keep the existing package separation: Transport, Protocol, Commands, Tools, Util, Tests.
- Put behavior in the narrowest existing folder that owns it.
- Do not add central managers or broad service hubs.
- Do not add external Unity packages unless the task explicitly needs them.
- Do not assume Addressables, UniTask, VContainer, Zenject, or other optional packages exist.
- Do not use `switch`.

## Tool Work

- Use `unified-unity-mcp` for tool names, schemas, catalog sync, and smoke checks.
- Validate input before touching Unity state.
- Marshal Unity API calls to the main thread.
- Return structured errors across the MCP boundary.
- Keep examples generic for open-source users.

## Done Criteria

- C# compiles or the compile blocker is reported exactly.
- Tool catalogs and relevant skills are updated for public MCP changes.
- Tests or smoke checks match the changed behavior.
