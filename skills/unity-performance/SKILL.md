---
name: unity-performance
description: Profile and optimize Unity projects without overengineering. Use for frame rate issues, editor tool slowness, allocation spikes, heavy scene scans, build size, or import bottlenecks.
---

# Unity Performance

## Use This Skill When

- A Unity Editor tool, scene operation, or target project workflow is slow.
- Console, profiler, or tests show allocations or long operations.
- Optimizing build, import, scene, UI, or asset workflows.

## Workflow

1. Identify the measured bottleneck or likely hot path.
2. Check the smallest relevant scope first.
3. Prefer direct Unity API improvements over architecture changes.
4. Re-run the same check after the change.

## Rules

- Do not optimize cold paths by adding complexity.
- Avoid allocations in repeated scans and frame-sensitive code.
- Cache only when repeated lookup cost is real.
- Use collection pools only for measured temporary allocations.
- Do not add broad managers, global caches, or new dependencies by default.
- Do not use `switch`.

## Report

- State the bottleneck or assumption.
- State what changed.
- State what was measured or which check could not run.
