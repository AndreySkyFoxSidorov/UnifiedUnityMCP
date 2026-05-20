---
name: skill-creator
description: Create or update concise Codex-style skills for Unified Unity MCP. Use when adding a new skill, rewriting an old skill, or validating skill structure.
---

# Skill Creator

## Use This Skill When

- Creating a new folder under `skills/`.
- Rewriting an existing `SKILL.md`.
- Splitting long guidance into references.
- Checking skill triggers and tool accuracy.

## Skill Shape

Each active skill must have:

```text
skills/<skill-name>/
  SKILL.md
  references/        optional
  scripts/           optional, only for reusable real helpers
  assets/            optional
```

`SKILL.md` starts with:

```yaml
---
name: skill-name
description: Concrete description of what the skill does and when to use it.
---
```

## Writing Rules

- Keep `SKILL.md` short and directly actionable.
- Put global project policy in `AGENTS.md`, not in skills.
- Avoid old-style personality rules, mandatory subagents, mandatory task files, or generic excellence language.
- Do not duplicate long reference material in the main skill.
- Do not describe tools that do not exist in the current MCP catalog.
- Do not add scripts unless they are reusable and worth maintaining.
- Keep text UTF-8 and English for public project docs.

## Validation

- Frontmatter has `name` and `description`.
- Description has clear triggers.
- Tool names match `active_tools.json` or `ToolsCatalog.md`.
- Skill content does not conflict with `AGENTS.md`.
- References are current or clearly marked as background.

## Helper Scripts

Existing helper scripts under `scripts/` are optional maintenance utilities. Before using them, inspect their behavior and make sure they match current repo rules.
