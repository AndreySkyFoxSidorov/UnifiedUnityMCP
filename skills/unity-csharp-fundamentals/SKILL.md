---
name: unity-csharp-fundamentals
description: Safe Unity C# fundamentals for this MCP project. Use when writing MonoBehaviours, ScriptableObjects, Editor code, components, or examples that touch Unity APIs.
---

# Unity C# Fundamentals

## Use This Skill When

- Writing Unity Editor code or Unity-facing examples.
- Adding components, ScriptableObjects, asset operations, scene operations, or editor utilities.
- Reviewing Unity API usage for safety.

## Core Rules

- Follow `AGENTS.md` and `csharp-code-style`.
- Keep Unity API calls on the main thread unless Unity explicitly documents otherwise.
- Use `[SerializeField] private` fields for inspector configuration when needed.
- Use `[RequireComponent]` when a component has a hard runtime dependency.
- Use `TryGetComponent` when the component may be absent.
- Cache repeated component lookups only when the lookup happens repeatedly or in a hot path.
- Avoid hidden global lookups unless the task requires project-wide discovery.
- Do not use `switch`.

## Lifecycle

- Keep `Awake`, `OnEnable`, `Start`, `Update`, and `OnDisable` small.
- Subscribe in `OnEnable` and unsubscribe in `OnDisable` when the object lifetime allows it.
- Use `OnDestroy` for cleanup that must happen even if the object was never enabled.
- Do not start async work or coroutines without a cancellation or stop path.

## Editor Code

- Editor-only code may use `UnityEditor` and belongs in an Editor assembly or Editor folder.
- Validate assets, scenes, and object IDs before mutating them.
- Prefer Unity APIs over hand-editing serialized text.
- Refresh live instance IDs after compile, scene load, domain reload, or asset import.

## References

Legacy reference files remain under `references/`. Use only parts that match current `AGENTS.md`.
