---
name: unity-testrunner
description: Run and interpret Unity Test Framework checks for Unified Unity MCP. Use when validating C# changes, MCP tools, editor behavior, or CI test output.
---

# Unity Test Runner

## Use This Skill When

- Running EditMode or PlayMode tests.
- Adding tests for MCP tools, protocol behavior, or editor utilities.
- Interpreting Unity test XML, logs, or CI failures.

## Rules

- Use the narrowest test scope that proves the change.
- Prefer EditMode tests for editor tools, protocol, catalogs, and pure C# behavior.
- Use PlayMode only when runtime scene behavior requires it.
- Do not require TDD by default.
- Do not add mocking frameworks or DI containers unless already present and required.
- Do not use `switch`.

## MCP Validation

For tool contract changes, verify:

- `initialize`
- `tools/list`
- target `tools/call`
- console error output
- updated catalog entries

## Result Summary

Report:

- command or tool used
- pass/fail count when available
- failing test names and first useful error
- any blocker that prevented tests from running

## References

Reference files contain CLI and parsing examples. Update stale shell snippets before copying them.
