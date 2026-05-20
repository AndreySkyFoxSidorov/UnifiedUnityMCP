# Skills Rules

## Scope

Rules for all skill folders under `skills/`.

## Active Skill Contract

- Each active skill lives in `skills/<skill-name>/SKILL.md`.
- `SKILL.md` must start with frontmatter containing `name` and `description`.
- Descriptions must say when to use the skill. Avoid vague claims such as "expert", "master", or "use proactively" without a concrete trigger.
- Keep the main `SKILL.md` short enough to read before work starts.
- Put long examples and reference tables under `references/` only when they are current and useful.
- Reference files are background material. They never override repo `AGENTS.md`.

## Style

- Skills must reinforce the repo style: simple Unity C#, no `switch`, no unnecessary abstractions, English comments, UTF-8 text.
- Do not encode project-specific naming rules such as `mPascalCase`, `bBoolean`, or enum prefixes unless the codebase actually uses them.
- Do not require TDD, subagents, task files, enterprise patterns, SOLID ceremony, Addressables, UniTask, VContainer, Zenject, or other dependencies by default.
- Mention optional packages only as optional and only after checking the target project has them.
- Prefer task-specific workflow over generic motivational rules.

## MCP Accuracy

- Tool references must match `Assets/ThirdParty/UnifiedUnityMCP/Editor/active_tools.json` and `Assets/ThirdParty/UnifiedUnityMCP/Editor/ToolsCatalog.md`.
- If a skill mentions a tool action or argument, verify it against the live server or catalog before relying on it.
- When a tool contract changes, update the matching skill in the same task.

## Maintenance

- Remove or rewrite stale skill content instead of layering new exceptions on top.
- Preserve useful examples only when they follow current repo rules.
- If a reference remains useful but partially stale, the active `SKILL.md` must clearly route around the stale part.
