# Unity C# Language Limitations

## Overview

Unity supports C# 9.0 language features but runs on Mono/IL2CPP runtime, which lacks certain .NET 5+ runtime types. This causes some C# 9.0 features to be unavailable.

## init Accessor (Not Available)

### The Problem

The `init` accessor requires `System.Runtime.CompilerServices.IsExternalInit` type, which is only available in .NET 5+ runtime. Unity's Mono/IL2CPP runtime does not include this type.

```csharp
// ❌ COMPILE ERROR CS0518 in Unity
public class Player
{
    public string Name { get; private init; }  // Error: IsExternalInit not defined
    public int Health { get; init; }           // Error: IsExternalInit not defined
}
```

**Error Message**: `error CS0518: Predefined type 'System.Runtime.CompilerServices.IsExternalInit' is not defined or imported`

### Unity-Compatible Alternatives

#### Option 1: private set (Recommended)

Use `private set` for properties that should only be set internally:

```csharp
public class Player : MonoBehaviour
{
    [SerializeField] private string mName;

    public string Name
    {
        get => mName;
        private set => mName = value;
    }

    public void Initialize(string name)
    {
        Name = name;  // Allowed - internal access
    }
}

// External code
Player player = GetComponent<Player>();
player.Initialize("Hero");
// player.Name = "Villain";  // ❌ Compile error - private set
```

#### Option 2: Readonly Field + Property (True Immutability)

For truly immutable data, use readonly field with expression-bodied property:

```csharp
public class PlayerData
{
    private readonly string mName;
    private readonly int mMaxHealth;

    public string Name => mName;
    public int MaxHealth => mMaxHealth;

    public PlayerData(string name, int maxHealth)
    {
        mName = name;
        mMaxHealth = maxHealth;
    }
}

// Usage
PlayerData data = new PlayerData("Hero", 100);
// data.Name = "Villain";  // ❌ Compile error - no setter
```

#### Option 3: Constructor-Only Pattern for MonoBehaviour

For MonoBehaviour components that need initialization:

```csharp
public class Enemy : MonoBehaviour
{
    private EnemyConfig mConfig;
    private bool mbIsInitialized;

    public EnemyConfig Config => mConfig;
    public bool IsInitialized => mbIsInitialized;

    public void Initialize(EnemyConfig config)
    {
        Debug.Assert(!mbIsInitialized, "Enemy already initialized");
        mConfig = config;
        mbIsInitialized = true;
    }
}
```

## Records in Unity

Records are partially supported in Unity, but without `init` properties:

```csharp
// ❌ DOES NOT WORK - positional records use init internally
public record PlayerDto(string Name, int Health);

// ✅ WORKS - explicit properties with private set
public record PlayerDto
{
    public string Name { get; private set; }
    public int Health { get; private set; }

    public PlayerDto(string name, int health)
    {
        Name = name;
        Health = health;
    }
}

// ✅ ALTERNATIVE - use class with value equality
public class PlayerDto : IEquatable<PlayerDto>
{
    public string Name { get; }
    public int Health { get; }

    public PlayerDto(string name, int health)
    {
        Name = name;
        Health = health;
    }

    public bool Equals(PlayerDto other)
    {
        if (other == null) return false;
        return Name == other.Name && Health == other.Health;
    }

    public override bool Equals(object obj) => Equals(obj as PlayerDto);
    public override int GetHashCode() => HashCode.Combine(Name, Health);
}
```

## required Modifier (Not Available)

The C# 11 `required` modifier is also not available in Unity:

```csharp
// ❌ NOT AVAILABLE in Unity
public class Config
{
    public required string ApiKey { get; init; }
}

// ✅ Unity Alternative - validate in constructor
public class Config
{
    public string ApiKey { get; }

    public Config(string apiKey)
    {
        Debug.Assert(!string.IsNullOrEmpty(apiKey), "ApiKey is required");
        ApiKey = apiKey;
    }
}
```

## Available C# 9.0 Features in Unity

The following C# 9.0 features ARE available in Unity:

| Feature | Status | Notes |
|---------|--------|-------|
| Pattern matching enhancements | ✅ Available | `and`, `or`, `not`, relational patterns |
| Switch expressions | ✅ Available | Including property patterns |
| Target-typed new (`new()`) | ✅ Available | **Prohibited by POCU standards** |
| Covariant return types | ✅ Available | Override with more derived type |
| Static anonymous functions | ✅ Available | `static () => ...` |
| Top-level statements | ✅ Available | **Not recommended for Unity** |
| Native-sized integers | ✅ Available | `nint`, `nuint` |
| Function pointers | ✅ Available | Unsafe context only |

### Pattern Matching Examples (Unity Compatible)

```csharp
// Relational patterns
public string GetHealthStatus(int health)
{
    return health switch
    {
        <= 0 => "Dead",
        < 25 => "Critical",
        < 50 => "Wounded",
        < 75 => "Injured",
        _ => "Healthy"
    };
}

// Logical patterns
public bool IsValidDamage(int damage)
{
    return damage is > 0 and <= 9999;
}

// Property patterns
public float GetSpeedModifier(CharacterState state)
{
    return state switch
    {
        { IsStunned: true } => 0f,
        { IsSprinting: true, Stamina: > 0 } => 1.5f,
        { IsCrouching: true } => 0.5f,
        _ => 1.0f
    };
}
```

## Workaround: Adding IsExternalInit

While possible to add the `IsExternalInit` type manually, this is **NOT RECOMMENDED**:

```csharp
// ⚠️ NOT RECOMMENDED - May cause issues with IL2CPP and future Unity versions
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
```

**Why avoid this workaround**:
1. May conflict with future Unity updates
2. IL2CPP stripping may cause issues
3. Not officially supported by Unity
4. `private set` pattern is cleaner and more explicit

## Best Practices Summary

1. **Use `private set`**: For most cases where you need controlled mutability
2. **Use readonly fields**: For truly immutable data
3. **Avoid workarounds**: Don't add IsExternalInit manually
4. **Pattern matching**: Fully utilize available C# 9.0 pattern matching
5. **Validate in constructors**: Replace `required` with assertion-based validation
6. **Prefer classes over records**: Until Unity officially supports record features
