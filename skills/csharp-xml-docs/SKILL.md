---
name: csharp-xml-docs
description: Write concise XML documentation for public C# APIs. Use when documenting public tools, protocol types, package APIs, or non-obvious behavior.
---

# C# XML Documentation

## Use This Skill When

- Adding or updating public APIs that need consumer-facing docs.
- Documenting MCP tool classes, protocol DTOs, or extension points.
- Cleaning up stale or noisy XML docs.

## Rules

- Document public APIs when the behavior is not obvious from the name and type.
- Keep summaries short and specific.
- Use English.
- Do not document trivial private methods or obvious getters/setters.
- Do not add comments that merely repeat the code.
- Mention thread affinity, Unity main-thread requirements, side effects, and public contract behavior when relevant.
- Keep docs synchronized with actual tool schemas and response fields.

## Format

```csharp
/// <summary>
/// Runs a Unity Editor operation through the MCP tool boundary.
/// </summary>
/// <param name="arguments">Validated tool arguments.</param>
/// <returns>A structured MCP tool response.</returns>
```

## References

The `references/` folder contains older examples. Use only examples that follow current `AGENTS.md` style.
