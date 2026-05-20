---
name: csharp-code-style
description: Apply the Unified Unity MCP C# style. Use when writing, reviewing, or refactoring C# in this repository, especially before changing Unity Editor tools, protocol code, or shared helpers.
---

# C# Code Style

## Use This Skill When

- Writing or refactoring C# in this repository.
- Reviewing code for method shape, naming, flow, or unnecessary abstraction.
- Updating examples in docs or skills.

## Binding Rules

Follow the repository `AGENTS.md` first.

- Keep C# simple, direct, and readable.
- Prefer explicit `if`/`if else` at the call site.
- Do not use `switch` statements, switch expressions, pattern switches, or switch-like operators.
- Keep methods compact and readable top to bottom.
- Add methods only for large logic, reused logic, or clear business actions.
- Inline one-use null checks, bool checks, wrappers, trivial conversions, and tiny formatting logic.
- Avoid enterprise-style layers, generic helper classes, and manager overengineering.
- Use early returns for errors, blockers, invalid input, or invalid state only.
- Write code comments in English.

## Naming

- Match nearby code unless it violates `AGENTS.md`.
- Use clear descriptive names over prefixes or compact abbreviations.
- Do not introduce `mPascalCase`, `bBoolean`, enum prefixes, or other project-specific naming schemes unless the surrounding file already uses them and the task is only local cleanup.
- Keep public MCP names stable and documented.

## Review Checklist

- No `switch`.
- No new dependency without explicit need.
- No abstraction added only for cleanliness.
- No tiny one-use helper methods.
- Public tool contracts still match catalogs and skills.
- Tests or smoke checks match the risk of the change.

## References

The files under `references/` are legacy background notes. Use them only after checking they do not conflict with `AGENTS.md`; update stale examples before relying on them.
