# Collection Pool Fundamentals

## Table of Contents
- [Pool Architecture](#pool-architecture)
- [Built-in Pool Types](#built-in-pool-types)
- [Lifecycle Management](#lifecycle-management)
- [Thread Safety](#thread-safety)
- [Memory Considerations](#memory-considerations)

## Pool Architecture

### How Unity Pools Work

```
Pool Structure:
┌─────────────────────────────────────┐
│           Static Pool               │
├─────────────────────────────────────┤
│  Stack<T> (pooled instances)        │
│  ┌─────┬─────┬─────┬─────┐         │
│  │ T1  │ T2  │ T3  │ ... │         │
│  └─────┴─────┴─────┴─────┘         │
├─────────────────────────────────────┤
│  Get() → Pop from stack (or create) │
│  Release() → Push to stack          │
└─────────────────────────────────────┘

Lifecycle:
1. Get() called
   → Stack empty? Create new instance
   → Stack has item? Pop and return
2. Release() called
   → Clear collection contents
   → Push back to stack
```

### Collection Pool Implementation

```csharp
// Simplified internal structure
public static class ListPool<T>
{
    private static readonly ObjectPool<List<T>> s_Pool =
        new ObjectPool<List<T>>(
            createFunc: () => new List<T>(),
            actionOnGet: null,
            actionOnRelease: l => l.Clear(),
            actionOnDestroy: null,
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: 10000
        );

    public static List<T> Get() => s_Pool.Get();
    public static void Release(List<T> toRelease) => s_Pool.Release(toRelease);

    public static PooledObject<List<T>> Get(out List<T> value)
        => s_Pool.Get(out value);
}
```

## Built-in Pool Types

### ListPool<T>

```csharp
using UnityEngine.Pool;

// Basic Get/Release
List<int> list = ListPool<int>.Get();
// ... use list
ListPool<int>.Release(list);

// Using pattern (recommended)
using (ListPool<int>.Get(out List<int> list))
{
    list.Add(1);
    list.Add(2);
    Process(list);
} // Auto-released

// With initial capacity hint (via ObjectPool)
// Note: ListPool doesn't expose capacity, use ObjectPool<List<T>> for control
```

### HashSetPool<T>

```csharp
using UnityEngine.Pool;

// Duplicate detection
using (HashSetPool<int>.Get(out HashSet<int> seen))
{
    foreach (var id in ids)
    {
        if (!seen.Add(id))
            Debug.Log($"Duplicate ID: {id}");
    }
}

// Set operations
using (HashSetPool<string>.Get(out HashSet<string> setA))
using (HashSetPool<string>.Get(out HashSet<string> setB))
{
    setA.UnionWith(collection1);
    setB.UnionWith(collection2);

    setA.IntersectWith(setB); // In-place intersection
    ProcessIntersection(setA);
}
```

### DictionaryPool<TKey, TValue>

```csharp
using UnityEngine.Pool;

// Temporary lookup
using (DictionaryPool<int, GameObject>.Get(out var lookup))
{
    foreach (var obj in objects)
        lookup[obj.GetInstanceID()] = obj;

    // O(1) lookup
    if (lookup.TryGetValue(targetId, out var found))
        Process(found);
}

// Grouping pattern
using (DictionaryPool<string, List<Item>>.Get(out var groups))
{
    foreach (var item in items)
    {
        if (!groups.TryGetValue(item.Category, out var list))
        {
            list = ListPool<Item>.Get();
            groups[item.Category] = list;
        }
        list.Add(item);
    }

    ProcessGroups(groups);

    // Release nested lists
    foreach (var list in groups.Values)
        ListPool<Item>.Release(list);
}
```

### CollectionPool<TCollection, TItem>

```csharp
using UnityEngine.Pool;

// For custom collection types implementing ICollection<T>
using (CollectionPool<Queue<int>, int>.Get(out Queue<int> queue))
{
    queue.Enqueue(1);
    queue.Enqueue(2);
    ProcessQueue(queue);
}

// Stack pooling
using (CollectionPool<Stack<string>, string>.Get(out Stack<string> stack))
{
    stack.Push("first");
    stack.Push("second");
    ProcessStack(stack);
}
```

### ObjectPool<T>

```csharp
using UnityEngine.Pool;

// Custom object pooling
public class BulletPool : MonoBehaviour
{
    private ObjectPool<Bullet> _pool;

    void Awake()
    {
        _pool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            collectionCheck: true,    // Debug: detect double-release
            defaultCapacity: 100,
            maxSize: 1000
        );
    }

    Bullet CreateBullet()
    {
        var go = Instantiate(bulletPrefab);
        go.TryGetComponent<Bullet>(out var bullet);
        return bullet;
    }

    void OnGetBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(true);
        bullet.Reset();
    }

    void OnReleaseBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    void OnDestroyBullet(Bullet bullet)
    {
        Destroy(bullet.gameObject);
    }

    public Bullet Get() => _pool.Get();
    public void Release(Bullet bullet) => _pool.Release(bullet);
}
```

### LinkedPool<T>

```csharp
// Linked list-based pool (different memory characteristics)
// Better for frequent resize, worse for cache locality

var linkedPool = new LinkedPool<MyClass>(
    createFunc: () => new MyClass(),
    actionOnGet: obj => obj.Initialize(),
    actionOnRelease: obj => obj.Cleanup(),
    actionOnDestroy: obj => obj.Dispose(),
    collectionCheck: true,
    maxSize: 100
);

var obj = linkedPool.Get();
// use obj
linkedPool.Release(obj);
```

### GenericPool<T>

```csharp
// Static shared pool for types with parameterless constructor
// Simplest usage for common scenarios

public class MyPooledClass { }

// Usage
var instance = GenericPool<MyPooledClass>.Get();
// use instance
GenericPool<MyPooledClass>.Release(instance);
```

## Lifecycle Management

### PooledObject<T> Pattern

```csharp
// PooledObject<T> implements IDisposable
// Automatically releases on dispose

public readonly struct PooledObject<T> : IDisposable where T : class
{
    private readonly T _toReturn;
    private readonly IObjectPool<T> _pool;

    public void Dispose() => _pool.Release(_toReturn);
}

// Usage ensures proper cleanup
using (var pooled = ListPool<int>.Get(out var list))
{
    // If exception occurs, list still returned to pool
    ThrowingMethod(list);
}
```

### Nested Pool Usage

```csharp
void ComplexOperation()
{
    // Outer pool
    using (ListPool<List<int>>.Get(out var outer))
    {
        for (int i = 0; i < 10; i++)
        {
            // Inner pool - careful with nesting!
            var inner = ListPool<int>.Get();
            inner.Add(i);
            outer.Add(inner);
        }

        ProcessNested(outer);

        // Must release inner lists manually
        foreach (var inner in outer)
            ListPool<int>.Release(inner);
    }
}

// Better pattern: avoid nested pooling when possible
void BetterComplexOperation()
{
    using (ListPool<int>.Get(out var combined))
    {
        for (int i = 0; i < 10; i++)
            combined.Add(i);

        Process(combined);
    }
}
```

### Exception Safety

```csharp
// SAFE: using pattern handles exceptions
using (ListPool<int>.Get(out var list))
{
    list.Add(1);
    throw new Exception(); // list still released
}

// SAFE: try/finally for manual management
var list = ListPool<int>.Get();
try
{
    ProcessWithExceptions(list);
}
finally
{
    ListPool<int>.Release(list);
}

// UNSAFE: No exception handling
var list = ListPool<int>.Get();
ProcessWithExceptions(list); // Exception = leak!
ListPool<int>.Release(list);
```

## Thread Safety

### Built-in Pool Thread Safety

```csharp
// Unity's built-in pools are thread-safe
// Internal locking protects the stack

// Safe: concurrent access
Parallel.For(0, 100, i =>
{
    using (ListPool<int>.Get(out var list))
    {
        list.Add(i);
        Process(list);
    }
});
```

### Custom Pool Thread Safety

```csharp
// ObjectPool<T> constructor parameter
var pool = new ObjectPool<MyClass>(
    createFunc: () => new MyClass(),
    collectionCheck: true,  // Thread-safe double-release detection
    // ...
);

// For high-contention scenarios, consider per-thread pools
[ThreadStatic]
private static ObjectPool<MyClass> s_ThreadPool;

private static ObjectPool<MyClass> GetThreadPool()
{
    return s_ThreadPool ??= new ObjectPool<MyClass>(
        () => new MyClass(),
        defaultCapacity: 10,
        maxSize: 100
    );
}
```

### Main Thread Considerations

```csharp
// Unity API calls must happen on main thread
// Pool itself is thread-safe, but pooled objects may not be

// Background thread
Task.Run(() =>
{
    using (ListPool<int>.Get(out var list))
    {
        // Safe: pure data operations
        for (int i = 0; i < 1000; i++)
            list.Add(i);

        // UNSAFE: Unity API on background thread
        // TryGetComponent<T>() // Would crash!
    }
});
```

## Memory Considerations

### Pool Capacity

```csharp
// Default pool settings
// defaultCapacity: Initial stack size
// maxSize: Maximum pooled instances

var pool = new ObjectPool<List<int>>(
    createFunc: () => new List<int>(),
    defaultCapacity: 10,    // Pre-allocate 10 slots
    maxSize: 100            // Never pool more than 100
);

// Excess objects are destroyed, not pooled
// Prevents unbounded memory growth
```

### Collection Capacity Retention

```csharp
// IMPORTANT: Collections retain their capacity after release
// This can be good (avoids resize) or bad (memory retention)

using (ListPool<int>.Get(out var list))
{
    // Add many items - list grows capacity
    for (int i = 0; i < 10000; i++)
        list.Add(i);
} // Released with Capacity = 10000+

// Next Get() returns list with large capacity
// Memory stays allocated until pool max exceeded

// Mitigation: Trim excess capacity if needed
using (ListPool<int>.Get(out var list))
{
    // ... use list
    if (list.Capacity > 1000)
        list.TrimExcess();
}
```

### Profiling Pool Usage

```csharp
// Track pool statistics with custom wrapper
public static class PoolStats<T>
{
    public static int TotalGets;
    public static int TotalReleases;
    public static int CurrentOut => TotalGets - TotalReleases;

    public static List<T> Get()
    {
        Interlocked.Increment(ref TotalGets);
        return ListPool<T>.Get();
    }

    public static void Release(List<T> list)
    {
        Interlocked.Increment(ref TotalReleases);
        ListPool<T>.Release(list);
    }
}

// Monitor in editor
void OnGUI()
{
    GUILayout.Label($"Pool<int> out: {PoolStats<int>.CurrentOut}");
}
```

### Memory Layout Comparison

```yaml
Stack-based Pool (ObjectPool):
  Pros:
    - Simple implementation
    - Good cache locality for pool operations
    - O(1) Get/Release
  Cons:
    - Contiguous memory for stack array
    - Resize allocates

Linked Pool (LinkedPool):
  Pros:
    - No resize allocations
    - Good for variable size
  Cons:
    - Worse cache locality
    - Node allocations (though pooled internally)
    - Extra indirection

Recommendation:
  - Use ObjectPool for most cases
  - Use LinkedPool for highly variable pool sizes
```
