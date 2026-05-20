# Component Access Patterns

## Table of Contents
- [TryGetComponent Deep Dive](#trygetcomponent-deep-dive)
- [Component Search Methods](#component-search-methods)
- [Caching Strategies](#caching-strategies)
- [Performance Comparisons](#performance-comparisons)
- [Advanced Patterns](#advanced-patterns)

## TryGetComponent Deep Dive

### Basic Usage

```csharp
// Standard pattern
if (TryGetComponent<Rigidbody>(out var rb))
{
    rb.velocity = Vector3.zero;
}

// With else handling
if (TryGetComponent<AudioSource>(out var audio))
{
    audio.Play();
}
else
{
    Debug.LogWarning($"No AudioSource on {gameObject.name}");
}

// Non-generic version (for runtime types)
if (TryGetComponent(typeof(Collider), out var component))
{
    var collider = (Collider)component;
}
```

### On Other GameObjects

```csharp
// TryGetComponent on other objects
public void DamageTarget(GameObject target)
{
    if (target.TryGetComponent<IDamageable>(out var damageable))
    {
        damageable.TakeDamage(10);
    }
}

// With collision
void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.TryGetComponent<Enemy>(out var enemy))
    {
        enemy.OnHit();
    }
}

// With trigger
void OnTriggerEnter(Collider other)
{
    if (other.TryGetComponent<PickupItem>(out var item))
    {
        item.Collect(this);
    }
}
```

### Interface-Based Access

```csharp
// Access via interface
public interface IInteractable
{
    void Interact(Player player);
}

void CheckInteraction()
{
    if (Physics.Raycast(ray, out var hit))
    {
        if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
        {
            interactable.Interact(_player);
        }
    }
}

// Multiple interfaces
public interface IHittable { void OnHit(int damage); }
public interface IKnockbackable { void ApplyKnockback(Vector3 force); }

void AttackTarget(GameObject target)
{
    if (target.TryGetComponent<IHittable>(out var hittable))
    {
        hittable.OnHit(10);
    }

    if (target.TryGetComponent<IKnockbackable>(out var knockbackable))
    {
        knockbackable.ApplyKnockback(transform.forward * 100f);
    }
}
```

### Awake Initialization Pattern

```csharp
public class PlayerController : MonoBehaviour
{
    private Rigidbody _rb;
    private Animator _animator;
    private AudioSource _audio;

    void Awake()
    {
        // Required components - log error if missing
        if (!TryGetComponent(out _rb))
        {
            Debug.LogError($"[{nameof(PlayerController)}] Missing Rigidbody!", this);
            enabled = false;
            return;
        }

        // Optional components - just check
        TryGetComponent(out _animator);
        TryGetComponent(out _audio);
    }

    void Update()
    {
        // _rb is guaranteed (or component is disabled)
        _rb.AddForce(Vector3.up);

        // Optional components need null check
        _animator?.SetFloat("Speed", _rb.velocity.magnitude);
    }
}
```

## Component Search Methods

### Hierarchy Search Patterns

```csharp
public class ComponentFinder : MonoBehaviour
{
    // Self only
    void FindOnSelf()
    {
        if (TryGetComponent<Enemy>(out var enemy)) { }
    }

    // Children (includes self)
    void FindInChildren()
    {
        var enemy = GetComponentInChildren<Enemy>();
        if (enemy != null) { }

        // Include inactive children
        var allEnemies = GetComponentInChildren<Enemy>(includeInactive: true);
    }

    // Parent (includes self)
    void FindInParent()
    {
        var controller = GetComponentInParent<GameController>();
        if (controller != null) { }

        // Include inactive parents
        var inactive = GetComponentInParent<GameController>(includeInactive: true);
    }

    // Multiple components
    void FindMultiple()
    {
        // On self - returns array
        var colliders = GetComponents<Collider>();

        // In children
        var childColliders = GetComponentsInChildren<Collider>();

        // In parent
        var parentColliders = GetComponentsInParent<Collider>();
    }
}
```

### Allocation-Free Multiple Component Access

```csharp
using UnityEngine.Pool;

public class AllocationFreeSearch : MonoBehaviour
{
    void ProcessChildren()
    {
        // AVOID: Creates new array each call
        var enemies = GetComponentsInChildren<Enemy>(); // Allocates!

        // BETTER: Reuse list
        using (ListPool<Enemy>.Get(out var enemyList))
        {
            GetComponentsInChildren(enemyList);
            foreach (var enemy in enemyList)
            {
                enemy.Alert();
            }
        }
    }

    // With cached list (for frequent calls)
    private readonly List<Enemy> _enemyCache = new();

    void ProcessChildrenCached()
    {
        _enemyCache.Clear();
        GetComponentsInChildren(_enemyCache);
        foreach (var enemy in _enemyCache)
        {
            enemy.Alert();
        }
    }
}
```

### Global Search Patterns (Unity 2023.1+)

```csharp
public class GlobalSearch : MonoBehaviour
{
    void FindGlobally()
    {
        // PREFERRED: FindAnyObjectByType - fastest when order doesn't matter
        var manager = FindAnyObjectByType<GameManager>();

        // ALTERNATIVE: FindFirstObjectByType - when deterministic order needed
        var orderedManager = FindFirstObjectByType<GameManager>();

        // MULTIPLE: FindObjectsByType with explicit sort mode
        var allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None); // Fastest
        var sortedEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.InstanceID);

        // With inactive objects
        var includeInactive = FindAnyObjectByType<GameManager>(
            FindObjectsInactive.Include);

        // OBSOLETE - DON'T USE:
        // FindObjectOfType<T>() - deprecated
        // FindObjectsOfType<T>() - deprecated
    }
}
```

## Caching Strategies

### Simple Field Caching

```csharp
public class SimpleCaching : MonoBehaviour
{
    private Rigidbody _rb;
    private Transform _cachedTransform;

    void Awake()
    {
        TryGetComponent(out _rb);
        _cachedTransform = transform; // Even transform should be cached
    }

    void FixedUpdate()
    {
        _rb.AddForce(_cachedTransform.forward * 10f);
    }
}
```

### Lazy Initialization Caching

```csharp
public class LazyCaching : MonoBehaviour
{
    private Rigidbody _rb;
    private bool _rbCached;

    // Lazy-load only when needed
    private Rigidbody Rb
    {
        get
        {
            if (!_rbCached)
            {
                TryGetComponent(out _rb);
                _rbCached = true;
            }
            return _rb;
        }
    }

    void SomeMethod()
    {
        if (Rb != null)
        {
            Rb.AddForce(Vector3.up);
        }
    }
}
```

### Reference Holder Pattern

```csharp
[Serializable]
public class ComponentReferences
{
    [SerializeField] private Rigidbody rigidbody;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;

    public Rigidbody Rigidbody => rigidbody;
    public Animator Animator => animator;
    public AudioSource AudioSource => audioSource;

    public bool Validate(MonoBehaviour owner)
    {
        bool valid = true;

        if (rigidbody == null)
        {
            Debug.LogError("Missing Rigidbody reference!", owner);
            valid = false;
        }

        return valid;
    }
}

public class Player : MonoBehaviour
{
    [SerializeField] private ComponentReferences _refs;

    void Awake()
    {
        if (!_refs.Validate(this))
        {
            enabled = false;
        }
    }
}
```

## Performance Comparisons

### Benchmark Results

```yaml
Component Access Performance (per 10,000 calls):
  TryGetComponent (found):     ~0.5ms
  GetComponent:                ~0.6ms
  TryGetComponent (not found): ~0.3ms (no GC)
  GetComponent (not found):    ~0.5ms (may GC)
  Cached field access:         ~0.001ms

Hierarchy Search (100 children):
  GetComponentInChildren:      ~0.8ms
  GetComponentsInChildren:     ~1.2ms + allocation
  GetComponentsInChildren(list): ~1.0ms (no allocation)

Global Search (1000 objects):
  FindAnyObjectByType:         ~1.5ms (fastest)
  FindFirstObjectByType:       ~2ms
  FindObjectsByType(None):     ~6ms + allocation
  FindObjectsByType(InstanceID): ~8ms + allocation
  FindObjectOfType (OBSOLETE): ~3ms - DON'T USE
```

### When to Cache

```csharp
public class CachingGuidelines : MonoBehaviour
{
    // ALWAYS cache - used frequently
    private Transform _transform;
    private Rigidbody _rb;

    // Cache if used more than once
    private Animator _animator;

    // May skip caching - used only in rare events
    void OnDeath()
    {
        // Acceptable: one-time lookup
        if (TryGetComponent<DeathEffect>(out var effect))
        {
            effect.Play();
        }
    }

    void Update()
    {
        // NEVER do this - lookup every frame
        // GetComponent<Rigidbody>().velocity = Vector3.zero; // BAD!

        // Use cached reference
        _rb.velocity = Vector3.zero;
    }
}
```

## Advanced Patterns

### Component Query Builder

```csharp
public static class ComponentQuery
{
    public static T FindFirst<T>(GameObject root, bool includeInactive = false)
        where T : class
    {
        // Try self first
        if (root.TryGetComponent<T>(out var self))
            return self;

        // Then children
        var child = root.GetComponentInChildren<T>(includeInactive);
        if (child != null)
            return child;

        // Then parent
        return root.GetComponentInParent<T>(includeInactive);
    }

    public static bool TryFindFirst<T>(GameObject root, out T component,
        bool includeInactive = false) where T : class
    {
        component = FindFirst<T>(root, includeInactive);
        return component != null;
    }
}

// Usage
if (ComponentQuery.TryFindFirst<IInteractable>(target, out var interactable))
{
    interactable.Interact(this);
}
```

### Safe Component Accessor

```csharp
public readonly struct SafeComponent<T> where T : Component
{
    private readonly T _component;
    public bool HasValue => _component != null;
    public T Value => _component;

    public SafeComponent(GameObject gameObject)
    {
        gameObject.TryGetComponent(out _component);
    }

    public void IfPresent(Action<T> action)
    {
        if (_component != null)
            action(_component);
    }

    public TResult Map<TResult>(Func<T, TResult> mapper, TResult defaultValue = default)
    {
        return _component != null ? mapper(_component) : defaultValue;
    }
}

// Usage
var rb = new SafeComponent<Rigidbody>(gameObject);
rb.IfPresent(r => r.velocity = Vector3.zero);
var speed = rb.Map(r => r.velocity.magnitude, 0f);
```

### Dependency Injection Fallback

```csharp
public class ServiceLocator : MonoBehaviour
{
    private IAudioService _audioService;

    void Awake()
    {
        // Try DI container first
        if (TryGetDependency(out _audioService))
            return;

        // Fallback to component search
        if (TryGetComponent(out AudioService service))
        {
            _audioService = service;
            return;
        }

        // Final fallback: create default
        _audioService = new NullAudioService();
    }

    private bool TryGetDependency<T>(out T service)
    {
        // Integration with VContainer, Zenject, etc.
        service = default;
        return false; // Placeholder
    }
}
```

### Editor Validation

```csharp
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PlayerController))]
public class PlayerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var player = (PlayerController)target;

        // Validate required components
        if (!player.TryGetComponent<Rigidbody>(out _))
        {
            EditorGUILayout.HelpBox("Missing Rigidbody!", MessageType.Error);
        }

        if (!player.TryGetComponent<Collider>(out _))
        {
            EditorGUILayout.HelpBox("Missing Collider!", MessageType.Warning);
        }
    }
}
#endif
```

### Component Registry Pattern

```csharp
public class ComponentRegistry : MonoBehaviour
{
    private static readonly Dictionary<Type, List<Component>> _registry = new();

    public static void Register<T>(T component) where T : Component
    {
        var type = typeof(T);
        if (!_registry.TryGetValue(type, out var list))
        {
            list = new List<Component>();
            _registry[type] = list;
        }
        list.Add(component);
    }

    public static void Unregister<T>(T component) where T : Component
    {
        if (_registry.TryGetValue(typeof(T), out var list))
        {
            list.Remove(component);
        }
    }

    public static bool TryGetFirst<T>(out T component) where T : Component
    {
        if (_registry.TryGetValue(typeof(T), out var list) && list.Count > 0)
        {
            component = (T)list[0];
            return true;
        }
        component = null;
        return false;
    }

    public static IReadOnlyList<T> GetAll<T>() where T : Component
    {
        if (_registry.TryGetValue(typeof(T), out var list))
        {
            return list.Cast<T>().ToList();
        }
        return Array.Empty<T>();
    }
}

// Self-registering component
public class Enemy : MonoBehaviour
{
    void OnEnable() => ComponentRegistry.Register(this);
    void OnDisable() => ComponentRegistry.Unregister(this);
}
```
