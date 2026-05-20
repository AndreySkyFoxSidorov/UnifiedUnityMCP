---
name: unity-collection-pool
description: Use Unity collection pools on measured hot paths. Use when reducing GC allocations from repeated temporary List, Dictionary, HashSet, or object creation in Unity code.
---

# Unity Collection Pool

## Use This Skill When

- Profiling shows repeated temporary collection allocations.
- A tool scans many Unity objects or assets repeatedly.
- A frame-sensitive path allocates lists, dictionaries, hash sets, or temporary objects.

## Rules

- Measure or identify a real hot path before adding pooling.
- Do not pool cold-path code just for style.
- Use `UnityEngine.Pool` only when the target Unity version/package supports it.
- Release pooled collections in `finally` when errors can happen.
- Clear pooled collections before release if the pool does not do it.
- Do not keep references to released pooled collections.
- Do not use `switch`.

## Preferred Shape

```csharp
List<GameObject> objects = ListPool<GameObject>.Get();
try
{
    // Fill and use the list.
}
finally
{
    ListPool<GameObject>.Release(objects);
}
```

## References

Reference files contain broader examples. Apply only examples that match current repo rules and available Unity packages.
