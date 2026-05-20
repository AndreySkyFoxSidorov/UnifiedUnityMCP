#!/usr/bin/env python3
"""
Validate the minimal structure of a skill folder.

Usage:
    python quick_validate.py <skill-directory>
"""

import re
import sys
from pathlib import Path


ALLOWED_KEYS = {
    "name",
    "description",
    "license",
    "allowed-tools",
    "metadata",
    "compatibility",
}


def parse_frontmatter(content):
    if not content.startswith("---\n"):
        return None, "No YAML frontmatter found"

    end_marker = "\n---"
    end_index = content.find(end_marker, 4)
    if end_index == -1:
        return None, "Invalid frontmatter format"

    frontmatter = {}
    lines = content[4:end_index].splitlines()

    for line in lines:
        if not line.strip():
            continue

        if ":" not in line:
            return None, f"Invalid frontmatter line: {line}"

        key, value = line.split(":", 1)
        frontmatter[key.strip()] = value.strip().strip("\"'")

    return frontmatter, None


def validate_name(name):
    if not isinstance(name, str) or not name:
        return "Missing skill name"

    if len(name) > 64:
        return "Skill name is too long"

    if not re.match(r"^[a-z0-9-]+$", name):
        return "Skill name must be kebab-case with lowercase letters, digits, and hyphens"

    if name.startswith("-") or name.endswith("-") or "--" in name:
        return "Skill name cannot start or end with a hyphen or contain consecutive hyphens"

    return None


def validate_skill(skill_path):
    skill_path = Path(skill_path)
    skill_md = skill_path / "SKILL.md"

    if not skill_path.is_dir():
        return False, "Skill path is not a directory"

    if not skill_md.exists():
        return False, "SKILL.md not found"

    content = skill_md.read_text(encoding="utf-8")
    frontmatter, error = parse_frontmatter(content)
    if error:
        return False, error

    unexpected_keys = set(frontmatter.keys()) - ALLOWED_KEYS
    if unexpected_keys:
        return False, "Unexpected frontmatter keys: " + ", ".join(sorted(unexpected_keys))

    name_error = validate_name(frontmatter.get("name", ""))
    if name_error:
        return False, name_error

    description = frontmatter.get("description", "")
    if not description:
        return False, "Missing description"

    if len(description) > 1024:
        return False, "Description is too long"

    if "<" in description or ">" in description:
        return False, "Description cannot contain angle brackets"

    return True, "Skill is valid"


def main():
    if len(sys.argv) != 2:
        print("Usage: python quick_validate.py <skill-directory>")
        return 1

    is_valid, message = validate_skill(sys.argv[1])
    print(message)
    return 0 if is_valid else 1


if __name__ == "__main__":
    sys.exit(main())
