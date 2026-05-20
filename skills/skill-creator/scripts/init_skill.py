#!/usr/bin/env python3
"""
Create a new skill folder for this repository.

Usage:
    python init_skill.py <skill-name> [skills-root]

Examples:
    python init_skill.py unity-my-workflow
    python init_skill.py unity-my-workflow ../../skills
"""

import re
import sys
from pathlib import Path


SKILL_TEMPLATE = """---
name: {skill_name}
description: TODO: Explain what this skill does and when to use it.
---

# {skill_title}

## Use This Skill When

- TODO: Add concrete trigger.

## Workflow

1. TODO: Add the first step.
2. TODO: Add the second step.
3. TODO: Add the verification step.

## Rules

- Follow repository AGENTS.md first.
- Keep this SKILL.md short and task-specific.
- Move long examples to references/ only when they are current and useful.
- Do not describe MCP tools that are not in the current catalog.
"""


def to_title(skill_name):
    return " ".join(part.capitalize() for part in skill_name.split("-"))


def is_valid_skill_name(skill_name):
    if len(skill_name) == 0 or len(skill_name) > 64:
        return False

    if skill_name.startswith("-") or skill_name.endswith("-"):
        return False

    if "--" in skill_name:
        return False

    return re.match(r"^[a-z0-9-]+$", skill_name) is not None


def create_skill(skill_name, skills_root):
    if not is_valid_skill_name(skill_name):
        print("Invalid skill name. Use lowercase kebab-case, digits, and hyphens only.")
        return 1

    root = Path(skills_root).resolve()
    skill_dir = root / skill_name

    if skill_dir.exists():
        print(f"Skill already exists: {skill_dir}")
        return 1

    skill_dir.mkdir(parents=True)
    skill_md = skill_dir / "SKILL.md"
    skill_md.write_text(
        SKILL_TEMPLATE.format(skill_name=skill_name, skill_title=to_title(skill_name)),
        encoding="utf-8",
        newline="\n",
    )

    print(f"Created {skill_md}")
    print("Next: edit SKILL.md, then run quick_validate.py on the skill folder.")
    return 0


def main():
    if len(sys.argv) < 2 or len(sys.argv) > 3:
        print("Usage: python init_skill.py <skill-name> [skills-root]")
        return 1

    skill_name = sys.argv[1]
    skills_root = sys.argv[2] if len(sys.argv) == 3 else Path(__file__).resolve().parents[2]
    return create_skill(skill_name, skills_root)


if __name__ == "__main__":
    sys.exit(main())
