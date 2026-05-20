# Agent Rules

## Scope

Repo-wide policy for Unified Unity MCP. Deeper `AGENTS.md` files may add local details for a subtree. On overlap, the deepest rule is most specific, but repo-wide policy still applies unless a repo-wide rule explicitly allows an exception.

Routes:

- `skills/AGENTS.md` - skill authoring and maintenance rules
- `Assets/ThirdParty/UnifiedUnityMCP/AGENTS.md` - Unity MCP package architecture
- `Assets/ThirdParty/UnifiedUnityMCP/Editor/AGENTS.md` - editor server, commands, tools, and catalogs
- `.agent/README.md` - optional agent client config notes
- `.gemini/README.md` - optional Gemini/Antigravity client config notes

## Repository Policy

- Unity 3D, C#.
- This is an open-source Unity Editor MCP server. Keep docs and examples generic; do not commit local machine paths, private endpoints, secrets, or user-specific client state.
- Code comments: English.
- Project text files: UTF-8. If a touched text file is ANSI/Windows-1252 or mojibake, convert it to UTF-8 before content edits.
- Prefer simple, direct, readable code.
- Unity C#: simple, flat, human-readable.
- Keep code small. Avoid unnecessary abstraction and indirection.
- Prefer explicit code over elegant architecture.
- Compact methods. Avoid deep nesting. Aim for 5-9 methods per class.
- Keep methods readable top to bottom. Keep logic near the execution point.
- Prefer direct `if`/`if/else` at the call site over one-use condition helpers.
- Inline simple or one-use logic. Prefer 2-5 duplicated lines over tiny extracted methods.
- Add a method only for large logic, reused logic, or a clear business action.
- Do not extract null checks, simple bool checks, one-line wrappers, UI toggle helpers, tiny formatting helpers, simple getters, save/load wrappers, or trivial conversions.
- No abstraction for cleanliness alone.
- No enterprise-style structure, utility layers, manager overengineering, or generic helper classes.
- Avoid many tiny methods and over-separated UI, validation, formatting, and state update logic.
- Do not add `switch` in any form: switch statement, switch expression, pattern switch, or switch-like operator.
- Use early returns only for errors, blockers, invalid input, or invalid state handling. Avoid normal success-path mid-method returns; keep scoped linear flow.
- Do not add dependencies or Unity packages unless the task explicitly requires them.

## Skills

- Active skills live in repo-root `skills/`.
- `.agent` is for client config only; do not put active rules or skills there.
- Before changing code, check whether a relevant `skills/<name>/SKILL.md` exists.
- Skills provide task workflow and reference context. `AGENTS.md` files provide binding project rules.
- If a skill conflicts with `AGENTS.md`, follow `AGENTS.md` and fix the skill in the same task when the conflict is in scope.

## MCP Tool Contract

- Tool names, schemas, and behavior must match the actual Unity MCP server.
- Prefer `tools/list`, `Assets/ThirdParty/UnifiedUnityMCP/Editor/active_tools.json`, and `Assets/ThirdParty/UnifiedUnityMCP/Editor/ToolsCatalog.md` as tool sources of truth.
- Do not invent MCP tool names, actions, arguments, status values, or protocol behavior.
- When changing a tool contract, update code, tests, tool catalogs, README snippets, and relevant skills in the same task.
- Public MCP behavior must remain backward-compatible unless a breaking change is explicitly requested and documented.

## Unity Automation

- Prefer MCP tools for Unity Editor object, scene, asset, console, build, and test operations when the server is available and the tool matches the task.
- Normal file edits are allowed for source and documentation work.
- Temporary scripts made only to modify scene, prefab, or serialized assets must be deleted after the edit.
- Do not create duplicate or temporary Unity project copies to bypass compile blocks, dirty scenes, or unsaved editor state.
- If Unity cannot safely compile, reimport, or run because of dirty scenes, missing access, or unexpected editor state, stop and report the exact blocker.

## Verification

- Documentation-only changes: verify file layout, links or referenced paths, and stale references with search.
- C# changes: compile or run the narrowest useful Unity test/smoke path.
- MCP contract changes: run or document the matching `tools/list`, smoke, and catalog verification.
- Do not report success for checks that were not run.

## AGENTS Maintenance

- Keep factual status blocks current if they are present in any `AGENTS.md`.
- If a task changes project structure, tool locations, or public MCP contracts, update the affected `AGENTS.md`, README, catalog, and skills in the same task.
