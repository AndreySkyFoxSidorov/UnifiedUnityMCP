---
name: csharp-async-patterns
description: Async, cancellation, and error handling patterns for C# in Unified Unity MCP. Use when adding Task-based work, background operations, HTTP handling, editor polling, or cancellation paths.
---

# C# Async Patterns

## Use This Skill When

- Adding or reviewing `Task`, `async`, cancellation, timeout, or background work.
- Touching HTTP transport or long-running MCP tool execution.
- Bridging background request handling with Unity main-thread APIs.

## Rules

- Do not add async code unless the operation is truly asynchronous.
- Prefer `CancellationToken` for operations that can outlive the request, editor state, or object lifetime.
- Avoid `async void` except for Unity or .NET event handlers that require it.
- Keep Unity API calls on the Unity main thread.
- Do not call Unity APIs from HTTP/background threads.
- Do not introduce UniTask or third-party async libraries unless the project already depends on them and the task requires them.
- Do not use `switch`.

## Error Handling

- Catch errors at the protocol or tool boundary and return structured MCP errors.
- Preserve useful exception messages in logs, but do not leak secrets or local-only paths into public docs.
- Cancel cleanly when the client disconnects or the operation is no longer valid.

## ConfigureAwait

- In Unity Editor code, prioritize correct main-thread behavior over blanket `ConfigureAwait(false)`.
- Use `ConfigureAwait(false)` only in pure .NET helper code that does not resume onto Unity APIs.
- After background work, marshal back to Unity main thread before touching Unity objects.

## References

Legacy reference files under `references/` are background. Update any old naming or style examples before using them.
