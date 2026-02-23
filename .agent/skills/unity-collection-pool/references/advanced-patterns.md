# Advanced Pool Patterns

## Table of Contents
- [Custom ObjectPool Configuration](#custom-objectpool-configuration)
- [Pool Sizing Strategies](#pool-sizing-strategies)
- [Specialized Pool Patterns](#specialized-pool-patterns)
- [Integration Patterns](#integration-patterns)
- [Performance Profiling](#performance-profiling)

## Custom ObjectPool Configuration

### Full Configuration Options

```csharp
using UnityEngine.Pool;

public class ConfiguredPool<T> where T : class, new()
{
    private readonly ObjectPool<T> _pool;

    public ConfiguredPool(
        int defaultCapacity = 10,
        int maxSize = 1000,
        bool collectionCheck = true)
    {
        _pool = new ObjectPool<T>(
            createFunc: () => new T(),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroy,
            collectionCheck: collectionCheck,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    protected virtual void OnGet(T item) { }
    protected virtual void OnRelease(T item) { }
    protected virtual void OnDestroy(T item) { }

    public T Get() => _pool.Get();
    public PooledObject<T> Get(out T item) => _pool.Get(out item);
    public void Release(T item) => _pool.Release(item);

    public int CountInactive => _pool.CountInactive;
    public int CountActive => _pool.CountActive;
    public int CountAll => _pool.CountAll;
}
```

### IObjectPool Interface Implementation

```csharp
// Custom pool implementing Unity's interface
public class BoundedPool<T> : IObjectPool<T> where T : class
{
    private readonly Stack<T> _stack;
    private readonly Func<T> _createFunc;
    private readonly Action<T> _onGet;
    private readonly Action<T> _onRelease;
    private readonly int _maxSize;
    private int _countActive;

    public BoundedPool(
        Func<T> createFunc,
        Action<T> onGet = null,
        Action<T> onRelease = null,
        int maxSize = 100)
    {
        _createFunc = createFunc;
        _onGet = onGet;
        _onRelease = onRelease;
        _maxSize = maxSize;
        _stack = new Stack<T>(maxSize);
    }

    public int CountInactive => _stack.Count;
    public int CountActive => _countActive;
    public int CountAll => CountInactive + CountActive;

    public T Get()
    {
        T item = _stack.Count > 0 ? _stack.Pop() : _createFunc();
        _onGet?.Invoke(item);
        _countActive++;
        return item;
    }

    public PooledObject<T> Get(out T v)
    {
        v = Get();
        return new PooledObject<T>(v, this);
    }

    public void Release(T item)
    {
        _onRelease?.Invoke(item);
        _countActive--;

        if (_stack.Count < _maxSize)
            _stack.Push(item);
        // else: discard (GC will collect)
    }

    public void Clear()
    {
        _stack.Clear();
        _countActive = 0;
    }
}
```

### Prefab Pool Pattern

```csharp
public class PrefabPool<T> : MonoBehaviour where T : Component
{
    [SerializeField] private T _prefab;
    [SerializeField] private Transform _poolContainer;
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxSize = 100;

    private ObjectPool<T> _pool;

    void Awake()
    {
        _pool = new ObjectPool<T>(
            createFunc: CreateInstance,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyPooled,
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxSize
        );

        // Pre-warm pool
        var prewarmed = new T[_defaultCapacity];
        for (int i = 0; i < _defaultCapacity; i++)
            prewarmed[i] = _pool.Get();
        foreach (var item in prewarmed)
            _pool.Release(item);
    }

    private T CreateInstance()
    {
        var instance = Instantiate(_prefab, _poolContainer);
        instance.gameObject.SetActive(false);
        return instance;
    }

    private void OnGetFromPool(T instance)
    {
        instance.gameObject.SetActive(true);
    }

    private void OnReleaseToPool(T instance)
    {
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(_poolContainer);
    }

    private void OnDestroyPooled(T instance)
    {
        if (instance != null)
            Destroy(instance.gameObject);
    }

    public T Spawn(Vector3 position, Quaternion rotation)
    {
        var instance = _pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public void Despawn(T instance)
    {
        _pool.Release(instance);
    }
}
```

## Pool Sizing Strategies

### Dynamic Pool Sizing

```csharp
public class AdaptivePool<T> where T : class, new()
{
    private ObjectPool<T> _pool;
    private int _maxSize;
    private int _peakUsage;
    private float _lastAdjustTime;
    private const float AdjustInterval = 5f;

    public AdaptivePool(int initialSize = 10, int initialMax = 100)
    {
        _maxSize = initialMax;
        RecreatePool(initialSize);
    }

    private void RecreatePool(int defaultCapacity)
    {
        _pool = new ObjectPool<T>(
            createFunc: () => new T(),
            defaultCapacity: defaultCapacity,
            maxSize: _maxSize
        );
    }

    public T Get()
    {
        var item = _pool.Get();
        _peakUsage = Mathf.Max(_peakUsage, _pool.CountActive);
        MaybeAdjustSize();
        return item;
    }

    public void Release(T item) => _pool.Release(item);

    private void MaybeAdjustSize()
    {
        if (Time.time - _lastAdjustTime < AdjustInterval) return;
        _lastAdjustTime = Time.time;

        // Grow if hitting limits frequently
        if (_peakUsage > _maxSize * 0.8f)
        {
            _maxSize = Mathf.Min(_maxSize * 2, 10000);
            Debug.Log($"Pool grown to maxSize: {_maxSize}");
        }
        // Shrink if over-provisioned
        else if (_peakUsage < _maxSize * 0.2f && _maxSize > 100)
        {
            _maxSize = Mathf.Max(_maxSize / 2, 100);
            Debug.Log($"Pool shrunk to maxSize: {_maxSize}");
        }

        _peakUsage = 0;
    }
}
```

### Warmup Strategies

```csharp
public static class PoolWarmup
{
    // Synchronous warmup
    public static void WarmupSync<T>(int count) where T : class
    {
        var items = new List<T>(count);
        for (int i = 0; i < count; i++)
            items.Add(ListPool<T>.Get() as T);

        foreach (var item in items)
            ListPool<T>.Release(item as List<T>);
    }

    // Async warmup (spread across frames)
    public static async UniTask WarmupAsync<T>(
        ObjectPool<T> pool,
        int count,
        int perFrame = 10,
        CancellationToken ct = default) where T : class
    {
        var items = new List<T>(count);

        for (int i = 0; i < count; i++)
        {
            items.Add(pool.Get());

            if (i % perFrame == 0)
                await UniTask.Yield(ct);
        }

        foreach (var item in items)
            pool.Release(item);
    }

    // Scene-based warmup
    public static void WarmupForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "Battle":
                WarmupSync<Bullet>(100);
                WarmupSync<DamageNumber>(50);
                WarmupSync<ParticleEffect>(30);
                break;
            case "Menu":
                WarmupSync<UIPanel>(10);
                break;
        }
    }
}
```

## Specialized Pool Patterns

### Component Pool with Auto-Return

```csharp
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

public class AutoReturnPool<T> where T : Component, IPoolable
{
    private readonly ObjectPool<T> _pool;
    private readonly Dictionary<T, float> _activeItems = new();

    public AutoReturnPool(T prefab, float autoReturnTime = 5f)
    {
        _pool = new ObjectPool<T>(
            createFunc: () => Object.Instantiate(prefab),
            actionOnGet: item =>
            {
                item.gameObject.SetActive(true);
                item.OnSpawn();
                _activeItems[item] = Time.time + autoReturnTime;
            },
            actionOnRelease: item =>
            {
                item.OnDespawn();
                item.gameObject.SetActive(false);
                _activeItems.Remove(item);
            }
        );
    }

    public T Get() => _pool.Get();
    public void Release(T item) => _pool.Release(item);

    public void Update()
    {
        using (ListPool<T>.Get(out var expired))
        {
            foreach (var kvp in _activeItems)
            {
                if (Time.time >= kvp.Value)
                    expired.Add(kvp.Key);
            }

            foreach (var item in expired)
                Release(item);
        }
    }
}
```

### Keyed Pool (Multiple Object Types)

```csharp
public class KeyedPool<TKey, TValue> where TValue : class
{
    private readonly Dictionary<TKey, ObjectPool<TValue>> _pools = new();
    private readonly Func<TKey, TValue> _createFunc;
    private readonly int _maxPerKey;

    public KeyedPool(Func<TKey, TValue> createFunc, int maxPerKey = 100)
    {
        _createFunc = createFunc;
        _maxPerKey = maxPerKey;
    }

    public TValue Get(TKey key)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            pool = new ObjectPool<TValue>(
                createFunc: () => _createFunc(key),
                maxSize: _maxPerKey
            );
            _pools[key] = pool;
        }
        return pool.Get();
    }

    public void Release(TKey key, TValue item)
    {
        if (_pools.TryGetValue(key, out var pool))
            pool.Release(item);
    }

    public void Clear()
    {
        foreach (var pool in _pools.Values)
            pool.Clear();
        _pools.Clear();
    }
}

// Usage: Pooling different enemy types
KeyedPool<string, Enemy> enemyPool = new(
    createFunc: type => EnemyFactory.Create(type)
);

var goblin = enemyPool.Get("goblin");
var dragon = enemyPool.Get("dragon");
enemyPool.Release("goblin", goblin);
```

### Scoped Pool (Auto-cleanup)

```csharp
public readonly struct PoolScope : IDisposable
{
    private readonly List<(object item, Action<object> release)> _items;

    public PoolScope(int expectedItems = 4)
    {
        _items = ListPool<(object, Action<object>)>.Get();
    }

    public List<T> GetList<T>()
    {
        var list = ListPool<T>.Get();
        _items.Add((list, obj => ListPool<T>.Release((List<T>)obj)));
        return list;
    }

    public HashSet<T> GetHashSet<T>()
    {
        var set = HashSetPool<T>.Get();
        _items.Add((set, obj => HashSetPool<T>.Release((HashSet<T>)obj)));
        return set;
    }

    public Dictionary<TKey, TValue> GetDictionary<TKey, TValue>()
    {
        var dict = DictionaryPool<TKey, TValue>.Get();
        _items.Add((dict, obj => DictionaryPool<TKey, TValue>.Release(
            (Dictionary<TKey, TValue>)obj)));
        return dict;
    }

    public void Dispose()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
            _items[i].release(_items[i].item);

        ListPool<(object, Action<object>)>.Release(_items);
    }
}

// Usage
using (var scope = new PoolScope())
{
    var list1 = scope.GetList<int>();
    var list2 = scope.GetList<string>();
    var dict = scope.GetDictionary<int, float>();

    // Use collections...

} // All released automatically
```

## Integration Patterns

### ECS/DOTS Compatible Pooling

```csharp
using Unity.Collections;
using Unity.Jobs;

// NativeArray pooling for Jobs
public static class NativeArrayPool<T> where T : struct
{
    private static readonly Dictionary<int, Stack<NativeArray<T>>> s_Pools = new();

    public static NativeArray<T> Get(int size, Allocator allocator = Allocator.TempJob)
    {
        // Round up to power of 2 for better pooling
        int poolSize = Mathf.NextPowerOfTwo(size);

        if (!s_Pools.TryGetValue(poolSize, out var stack))
        {
            stack = new Stack<NativeArray<T>>();
            s_Pools[poolSize] = stack;
        }

        if (stack.Count > 0)
        {
            var array = stack.Pop();
            return array;
        }

        return new NativeArray<T>(poolSize, allocator);
    }

    public static void Release(NativeArray<T> array)
    {
        if (!array.IsCreated) return;

        int poolSize = array.Length;
        if (!s_Pools.TryGetValue(poolSize, out var stack))
        {
            stack = new Stack<NativeArray<T>>();
            s_Pools[poolSize] = stack;
        }

        if (stack.Count < 10)
            stack.Push(array);
        else
            array.Dispose();
    }

    public static void DisposeAll()
    {
        foreach (var stack in s_Pools.Values)
        {
            while (stack.Count > 0)
                stack.Pop().Dispose();
        }
        s_Pools.Clear();
    }
}
```

### UniTask Integration

```csharp
using Cysharp.Threading.Tasks;

public static class AsyncPoolExtensions
{
    // Async pool operation with timeout
    public static async UniTask<T> GetWithTimeoutAsync<T>(
        this ObjectPool<T> pool,
        TimeSpan timeout,
        CancellationToken ct = default) where T : class
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfterSlim(timeout);

        try
        {
            // Pool.Get is synchronous, but we can yield to not block
            await UniTask.SwitchToThreadPool();
            var item = pool.Get();
            await UniTask.SwitchToMainThread(ct);
            return item;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("Pool.Get timed out");
        }
    }

    // Auto-return after delay
    public static async UniTaskVoid AutoReleaseAfterAsync<T>(
        this ObjectPool<T> pool,
        T item,
        float delaySeconds,
        CancellationToken ct = default) where T : class
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: ct);
        pool.Release(item);
    }
}
```

### Addressables Integration

```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablePool<T> where T : Component
{
    private readonly string _address;
    private ObjectPool<T> _pool;
    private AsyncOperationHandle<GameObject> _prefabHandle;

    public async UniTask InitializeAsync(string address)
    {
        _address = address;
        _prefabHandle = Addressables.LoadAssetAsync<GameObject>(address);
        var prefab = await _prefabHandle;

        _pool = new ObjectPool<T>(
            createFunc: () =>
            {
                var go = Object.Instantiate(prefab);
                go.TryGetComponent<T>(out var component);
                return component;
            },
            actionOnGet: item => item.gameObject.SetActive(true),
            actionOnRelease: item => item.gameObject.SetActive(false),
            actionOnDestroy: item => Object.Destroy(item.gameObject)
        );
    }

    public T Get() => _pool.Get();
    public void Release(T item) => _pool.Release(item);

    public void Dispose()
    {
        _pool.Clear();
        if (_prefabHandle.IsValid())
            Addressables.Release(_prefabHandle);
    }
}
```

## Performance Profiling

### Pool Metrics Collector

```csharp
public class PoolMetrics
{
    public string Name { get; }
    public int TotalGets { get; private set; }
    public int TotalReleases { get; private set; }
    public int TotalCreates { get; private set; }
    public int TotalDestroys { get; private set; }
    public int PeakActive { get; private set; }
    public float AverageActiveTime { get; private set; }

    private int _currentActive;
    private float _totalActiveTime;
    private readonly Dictionary<object, float> _getTimestamps = new();

    public PoolMetrics(string name) => Name = name;

    public void RecordGet(object item)
    {
        TotalGets++;
        _currentActive++;
        PeakActive = Mathf.Max(PeakActive, _currentActive);
        _getTimestamps[item] = Time.realtimeSinceStartup;
    }

    public void RecordRelease(object item)
    {
        TotalReleases++;
        _currentActive--;

        if (_getTimestamps.TryGetValue(item, out float getTime))
        {
            _totalActiveTime += Time.realtimeSinceStartup - getTime;
            AverageActiveTime = _totalActiveTime / TotalReleases;
            _getTimestamps.Remove(item);
        }
    }

    public void RecordCreate() => TotalCreates++;
    public void RecordDestroy() => TotalDestroys++;

    public override string ToString() =>
        $"Pool '{Name}': Gets={TotalGets}, Creates={TotalCreates}, " +
        $"Peak={PeakActive}, AvgTime={AverageActiveTime:F3}s";
}
```

### Profiler Integration

```csharp
using Unity.Profiling;

public class ProfiledPool<T> where T : class, new()
{
    private readonly ObjectPool<T> _pool;

    private static readonly ProfilerMarker s_GetMarker =
        new("Pool.Get");
    private static readonly ProfilerMarker s_ReleaseMarker =
        new("Pool.Release");
    private static readonly ProfilerCounterValue<int> s_ActiveCount =
        new("Pool Active", ProfilerCategory.Memory);

    public ProfiledPool()
    {
        _pool = new ObjectPool<T>(
            createFunc: () => new T(),
            actionOnGet: _ => s_ActiveCount.Value++,
            actionOnRelease: _ => s_ActiveCount.Value--
        );
    }

    public T Get()
    {
        using (s_GetMarker.Auto())
        {
            return _pool.Get();
        }
    }

    public void Release(T item)
    {
        using (s_ReleaseMarker.Auto())
        {
            _pool.Release(item);
        }
    }
}
```

### Debug Visualization

```csharp
#if UNITY_EDITOR
public class PoolDebugWindow : EditorWindow
{
    private static readonly List<(string name, Func<string> status)> s_Pools = new();

    public static void Register(string name, Func<string> statusFunc)
    {
        s_Pools.Add((name, statusFunc));
    }

    [MenuItem("Debug/Pool Status")]
    static void ShowWindow() => GetWindow<PoolDebugWindow>("Pool Status");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Active Pools", EditorStyles.boldLabel);

        foreach (var (name, status) in s_Pools)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, GUILayout.Width(150));
            EditorGUILayout.LabelField(status());
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Refresh"))
            Repaint();
    }
}
#endif
```
