---
name: unity-textmeshpro
description: TextMeshPro setup, review, and optimization for Unity UI and world text. Use when a target project uses TMPro or the task explicitly asks for TextMeshPro text, font assets, materials, or rich text.
---

# Unity TextMeshPro

## Use This Skill When

- Creating or editing TextMeshPro UI or world text.
- Reviewing font asset, fallback, material preset, or rich text setup.
- Reducing text rebuilds or font atlas issues.

## Rules

- Confirm TextMeshPro is installed or already used before depending on it.
- Use `TMP_Text` for code that can support both UI and world text.
- Avoid per-frame text string allocations in hot UI.
- Cache formatted text only when updates are frequent.
- Keep font fallback chains intentional and limited.
- Do not add Addressables, UniTask, or localization packages just for text unless the task requires them.
- Do not use `switch`.

## Checks

- Font asset supports required glyphs.
- Dynamic atlas growth is intentional.
- Text updates are not happening every frame unnecessarily.
- Materials and fallback assets are shared where appropriate.

## References

Reference files contain deeper TMPro notes. Apply only guidance that matches current project dependencies.
