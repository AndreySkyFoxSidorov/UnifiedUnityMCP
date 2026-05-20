#!/usr/bin/env python3
"""
Package a skill folder into a .skill zip archive.

Usage:
    python package_skill.py <path-to-skill-folder> [output-directory]

Examples:
    python package_skill.py ../../skills/unified-unity-mcp
    python package_skill.py ../../skills/unified-unity-mcp ./dist
"""

import sys
import zipfile
from pathlib import Path

from quick_validate import validate_skill


def package_skill(skill_path, output_dir=None):
    skill_path = Path(skill_path).resolve()

    if not skill_path.is_dir():
        print(f"Skill folder not found: {skill_path}")
        return None

    is_valid, message = validate_skill(skill_path)
    if not is_valid:
        print(f"Validation failed: {message}")
        return None

    output_path = Path(output_dir).resolve() if output_dir else Path.cwd()
    output_path.mkdir(parents=True, exist_ok=True)

    package_path = output_path / f"{skill_path.name}.skill"

    with zipfile.ZipFile(package_path, "w", zipfile.ZIP_DEFLATED) as zip_file:
        for file_path in skill_path.rglob("*"):
            if file_path.is_file():
                archive_name = file_path.relative_to(skill_path.parent)
                zip_file.write(file_path, archive_name)

    print(f"Packaged {package_path}")
    return package_path


def main():
    if len(sys.argv) < 2 or len(sys.argv) > 3:
        print("Usage: python package_skill.py <path-to-skill-folder> [output-directory]")
        return 1

    output_dir = sys.argv[2] if len(sys.argv) == 3 else None
    result = package_skill(sys.argv[1], output_dir)
    return 0 if result else 1


if __name__ == "__main__":
    sys.exit(main())
