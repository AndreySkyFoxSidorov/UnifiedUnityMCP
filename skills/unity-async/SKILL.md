---
name: unity-async
description: Unity async workflow guidance for coroutines, tasks, editor polling, and main-thread boundaries. Use when an operation crosses Unity Editor state, background HTTP work, or delayed execution.
---

# Unity Async

## Use This Skill When

- Bridging MCP HTTP requests to Unity Editor APIs.
- Waiting for compile, play mode, import, build, or test state.
- Adding cancellation or timeout behavior around Unity work.

## Rules

- Unity object, asset, scene, and editor APIs must run on the Unity main thread.
- Use the existing main-thread dispatcher before adding new dispatch infrastructure.
- Keep polling loops bounded with timeout or cancellation.
- Do not add UniTask or other async packages unless already present and required.
- Prefer the simplest Unity-native path that fits the task: direct call, `EditorApplication.delayCall`, coroutine, or `Task`.
- Do not use `switch`.

## Checks

- Operation cancels or times out cleanly.
- Domain reload, scene load, and compile state do not leave dangling work.
- Errors are returned through the MCP boundary in structured form.
