---
name: csharp-pro
description: General C# implementation and refactoring support aligned with Unified Unity MCP rules. Use for non-Unity-specific C# logic, protocol DTOs, JSON-RPC flow, and focused code reviews.
---

# C# Implementation Support

## Use This Skill When

- Implementing protocol, transport, command, or utility code.
- Refactoring non-Unity-specific C#.
- Reviewing error handling, data flow, or public API shape.

## Approach

- Start from `AGENTS.md` and `csharp-code-style`.
- Keep code direct and local to the behavior being changed.
- Prefer small concrete classes over broad frameworks.
- Prefer explicit types when they improve readability.
- Avoid records, pattern-heavy code, and clever expressions unless the surrounding Unity version and codebase already support them and they simplify real complexity.
- Do not introduce enterprise patterns, dependency injection frameworks, or generic service layers.
- Do not use `switch`.

## Async

- Use `csharp-async-patterns` for async work.
- Do not add async machinery unless the operation is actually asynchronous.
- Keep Unity API calls on the Unity main thread.

## Output Checklist

- Code compiles in the target Unity version.
- Public contracts are synchronized with docs and skills.
- No new dependencies were added without a direct requirement.
- Tests or smoke checks cover changed behavior.
