# Attributes and Patterns

## Table of Contents
- [Serialization Attributes](#serialization-attributes)
- [Inspector Customization](#inspector-customization)
- [Component Attributes](#component-attributes)
- [Execution Control](#execution-control)
- [Conditional Compilation](#conditional-compilation)

## Serialization Attributes

### Core Serialization

```csharp
public class SerializationExample : MonoBehaviour
{
    // Expose private field to Inspector
    [SerializeField]
    private float _speed = 5f;

    // Hide public field from Inspector
    [HideInInspector]
    public int InternalCounter;

    // Prevent serialization entirely
    [NonSerialized]
    public float RuntimeValue;

    // Serialize property backing field (Unity 2020.1+)
    [field: SerializeField]
    public float Health { get; private set; } = 100f;
}
```

### Rename Without Data Loss

```csharp
public class RefactoredClass : MonoBehaviour
{
    // Old field name was "speed", renamed to "_moveSpeed"
    [FormerlySerializedAs("speed")]
    [SerializeField]
    private float _moveSpeed = 5f;

    // Handles multiple renames in history
    [FormerlySerializedAs("hp")]
    [FormerlySerializedAs("health")]
    [SerializeField]
    private float _currentHealth = 100f;
}
```

### Serializable Classes

```csharp
[Serializable]
public class EnemyData
{
    public string Name;
    public int Health;
    public float Speed;
}

[Serializable]
public struct WeaponStats
{
    public int Damage;
    public float FireRate;
    public int AmmoCapacity;
}

public class GameManager : MonoBehaviour
{
    // Custom classes show in Inspector
    [SerializeField]
    private EnemyData _bossData;

    [SerializeField]
    private List<WeaponStats> _weapons;
}
```

### SerializeReference (Unity 2019.3+)

```csharp
public interface IAbility
{
    void Execute();
}

[Serializable]
public class FireballAbility : IAbility
{
    public float Damage = 10f;
    public void Execute() { /* ... */ }
}

[Serializable]
public class HealAbility : IAbility
{
    public float HealAmount = 20f;
    public void Execute() { /* ... */ }
}

public class Character : MonoBehaviour
{
    // Serialize interface/abstract references
    [SerializeReference]
    private IAbility _primaryAbility;

    [SerializeReference]
    private List<IAbility> _abilities;
}
```

## Inspector Customization

### Layout Attributes

```csharp
public class InspectorLayout : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _jumpForce = 10f;

    [Space(20)] // Vertical space

    [Header("Combat Settings")]
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private float _attackCooldown = 1f;
}
```

### Value Constraints

```csharp
public class ConstrainedValues : MonoBehaviour
{
    // Slider with min/max
    [Range(0f, 100f)]
    [SerializeField] private float _health = 100f;

    // Minimum value only
    [Min(0)]
    [SerializeField] private int _itemCount;

    // Delayed value (applies on Enter/focus lost)
    [Delayed]
    [SerializeField] private string _playerName;
}
```

### Text Fields

```csharp
public class TextFields : MonoBehaviour
{
    // Multi-line with scroll
    [TextArea(minLines: 3, maxLines: 10)]
    [SerializeField] private string _description;

    // Simple multi-line (fixed height)
    [Multiline(5)]
    [SerializeField] private string _notes;
}
```

### Tooltips and Help

```csharp
public class DocumentedFields : MonoBehaviour
{
    [Tooltip("Movement speed in units per second")]
    [SerializeField] private float _speed = 5f;

    [Tooltip("Maximum health points.\nCannot exceed 1000.")]
    [Range(1, 1000)]
    [SerializeField] private int _maxHealth = 100;

    // Context menu action
    [ContextMenuItem("Reset to Default", "ResetSpeed")]
    [SerializeField] private float _customSpeed = 5f;

    void ResetSpeed() => _customSpeed = 5f;
}
```

### Color and Gradient

```csharp
public class VisualSettings : MonoBehaviour
{
    // Color picker
    [ColorUsage(showAlpha: true, hdr: false)]
    [SerializeField] private Color _baseColor = Color.white;

    // HDR color (for emission, etc.)
    [ColorUsage(showAlpha: true, hdr: true)]
    [SerializeField] private Color _emissionColor;

    // Gradient
    [GradientUsage(hdr: false)]
    [SerializeField] private Gradient _fadeGradient;
}
```

## Component Attributes

### RequireComponent

```csharp
// Single requirement
[RequireComponent(typeof(Rigidbody))]
public class PhysicsController : MonoBehaviour
{
    private Rigidbody _rb;

    void Awake()
    {
        TryGetComponent(out _rb); // Guaranteed to exist
    }
}

// Multiple requirements
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class FullFeaturedObject : MonoBehaviour { }

// Combined in single attribute (max 3)
[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(AudioSource))]
public class AnotherObject : MonoBehaviour { }
```

### DisallowMultipleComponent

```csharp
// Only one instance per GameObject
[DisallowMultipleComponent]
public class GameController : MonoBehaviour { }

[DisallowMultipleComponent]
public class AudioManager : MonoBehaviour { }
```

### AddComponentMenu

```csharp
// Custom menu path
[AddComponentMenu("Game/Player/Player Controller")]
public class PlayerController : MonoBehaviour { }

// Remove from menu (hide component)
[AddComponentMenu("")]
public class InternalSystem : MonoBehaviour { }
```

### ExecuteInEditMode / ExecuteAlways

```csharp
// Run in editor (legacy)
[ExecuteInEditMode]
public class EditorPreview : MonoBehaviour
{
    void Update()
    {
        // Runs in edit mode
    }
}

// Run in editor AND play mode (preferred)
[ExecuteAlways]
public class AlwaysExecute : MonoBehaviour
{
    void Update()
    {
        if (Application.isPlaying)
        {
            // Play mode logic
        }
        else
        {
            // Edit mode logic (preview, etc.)
        }
    }
}
```

### SelectionBase

```csharp
// Click child selects this parent
[SelectionBase]
public class CharacterRoot : MonoBehaviour
{
    // Clicking any child in Scene view selects this object
}
```

## Execution Control

### DefaultExecutionOrder

```csharp
// Execute before default (negative = earlier)
[DefaultExecutionOrder(-100)]
public class EarlyInitializer : MonoBehaviour
{
    void Awake()
    {
        // Runs before other Awake methods
    }
}

// Execute after default (positive = later)
[DefaultExecutionOrder(100)]
public class LateInitializer : MonoBehaviour
{
    void Awake()
    {
        // Runs after other Awake methods
    }
}
```

### RuntimeInitializeOnLoadMethod

```csharp
public static class GameBootstrap
{
    // Before scene loads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitializeBeforeScene()
    {
        Debug.Log("Initializing before scene load");
    }

    // After scene loads (default)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeAfterScene()
    {
        Debug.Log("Initializing after scene load");
    }

    // Before splash screen
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void InitializeEarly()
    {
        // Very early initialization
    }

    // Subsystem registration (earliest)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void RegisterSubsystems()
    {
        // Register custom subsystems, reset statics
    }
}
```

### ContextMenu

```csharp
public class DebugActions : MonoBehaviour
{
    [SerializeField] private int _health = 100;

    // Appears in component context menu
    [ContextMenu("Reset Health")]
    void ResetHealth()
    {
        _health = 100;
    }

    [ContextMenu("Debug/Print State")]
    void PrintState()
    {
        Debug.Log($"Health: {_health}");
    }

    // Validation method
    [ContextMenu("Damage", isValidateFunction: false)]
    void Damage() => _health -= 10;

    [ContextMenu("Damage", isValidateFunction: true)]
    bool CanDamage() => _health > 0;
}
```

## Conditional Compilation

### Platform-Specific Code

```csharp
public class PlatformSpecific : MonoBehaviour
{
    void Start()
    {
#if UNITY_EDITOR
        Debug.Log("Running in Editor");
#elif UNITY_STANDALONE
        Debug.Log("Running on Desktop");
#elif UNITY_ANDROID
        Debug.Log("Running on Android");
#elif UNITY_IOS
        Debug.Log("Running on iOS");
#elif UNITY_WEBGL
        Debug.Log("Running in WebGL");
#endif
    }
}
```

### Development vs Release

```csharp
public class DebugSystem : MonoBehaviour
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void EditorLog(string message)
    {
        Debug.Log($"[Editor] {message}");
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void DebugLog(string message)
    {
        Debug.Log($"[Debug] {message}");
    }

    void Update()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        DrawDebugInfo();
#endif
    }

    void DrawDebugInfo()
    {
        // Only in development
    }
}
```

### Custom Scripting Symbols

```csharp
// Define in Player Settings > Scripting Define Symbols
// Example: ENABLE_CHEATS;VERBOSE_LOGGING

public class CheatSystem : MonoBehaviour
{
#if ENABLE_CHEATS
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            GodMode();
        }
    }

    void GodMode() { /* ... */ }
#endif
}

public class VerboseLogger : MonoBehaviour
{
    public static void Log(string message)
    {
#if VERBOSE_LOGGING
        Debug.Log($"[VERBOSE] {message}");
#endif
    }
}
```

### Feature Flags Pattern

```csharp
// AssemblyInfo.cs or separate file
#if UNITY_2021_3_OR_NEWER
#define HAS_AWAIT_SUPPORT
#endif

#if UNITY_2022_1_OR_NEWER
#define HAS_SERIALIZED_PROPERTY_PATH
#endif

public class VersionSpecific : MonoBehaviour
{
#if HAS_AWAIT_SUPPORT
    async void Start()
    {
        await Task.Delay(1000);
    }
#else
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
    }
#endif
}
```

## Advanced Attribute Patterns

### Custom Attributes

```csharp
// Read-only in Inspector
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif

// Usage
public class Example : MonoBehaviour
{
    [ReadOnly]
    [SerializeField] private float _computedValue;
}
```

### Attribute Combinations

```csharp
public class WellDocumentedComponent : MonoBehaviour
{
    [Header("Core Settings")]
    [Tooltip("Base movement speed in units/second")]
    [Range(0f, 20f)]
    [SerializeField]
    private float _baseSpeed = 5f;

    [Space(10)]

    [Header("Advanced Settings")]
    [Tooltip("Multiplier applied to base speed when sprinting")]
    [Min(1f)]
    [SerializeField]
    private float _sprintMultiplier = 1.5f;

    [Header("Debug")]
    [Tooltip("Current calculated speed (read-only)")]
    [SerializeField]
    private float _currentSpeed;

    [ContextMenu("Reset to Defaults")]
    void ResetDefaults()
    {
        _baseSpeed = 5f;
        _sprintMultiplier = 1.5f;
    }
}
```

### Validation Attributes

```csharp
// Scene reference validation
public class SceneRefAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneRefAttribute))]
public class SceneRefDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label);

        if (property.objectReferenceValue != null)
        {
            var path = AssetDatabase.GetAssetPath(property.objectReferenceValue);
            if (!path.EndsWith(".unity"))
            {
                EditorGUI.HelpBox(
                    new Rect(position.x, position.y + 20, position.width, 30),
                    "Must be a scene asset!", MessageType.Error);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) + 35;
    }
}
#endif
```

### Interface Serialization Pattern

```csharp
// Wrapper for interface serialization
[Serializable]
public class SerializableInterface<T> where T : class
{
    [SerializeField] private Object _object;

    public T Value
    {
        get => _object as T;
        set => _object = value as Object;
    }

    public bool HasValue => _object != null && _object is T;
}

// Usage
public class AbilityUser : MonoBehaviour
{
    [SerializeField]
    private SerializableInterface<IAbility> _mainAbility;

    void UseAbility()
    {
        if (_mainAbility.HasValue)
        {
            _mainAbility.Value.Execute();
        }
    }
}
```
