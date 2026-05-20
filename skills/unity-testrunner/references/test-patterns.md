# Unity Test Patterns

Comprehensive NUnit test patterns for Unity, covering EditMode, PlayMode, async testing, and mocking strategies.

## NUnit Attribute Reference

### Test Definition

| Attribute | Description | Example |
|-----------|-------------|---------|
| `[Test]` | Marks a test method | `[Test] public void MyTest()` |
| `[TestFixture]` | Marks a test class | `[TestFixture] public class MyTests` |
| `[TestCase]` | Parameterized test | `[TestCase(1, 2, 3)]` |
| `[TestCaseSource]` | External test data source | `[TestCaseSource(nameof(Cases))]` |

### Setup and Teardown

| Attribute | Scope | Timing |
|-----------|-------|--------|
| `[SetUp]` | Per test | Before each test |
| `[TearDown]` | Per test | After each test |
| `[OneTimeSetUp]` | Per fixture | Before first test in class |
| `[OneTimeTearDown]` | Per fixture | After last test in class |

### Test Control

| Attribute | Description |
|-----------|-------------|
| `[Ignore("reason")]` | Skip test with reason |
| `[Category("name")]` | Group tests for filtering |
| `[Timeout(ms)]` | Set test timeout |
| `[Repeat(n)]` | Run test n times |
| `[Order(n)]` | Control execution order |

### Unity-Specific

| Attribute | Description |
|-----------|-------------|
| `[UnityTest]` | Coroutine-based test (PlayMode/EditMode) |
| `[UnitySetUp]` | Coroutine setup |
| `[UnityTearDown]` | Coroutine teardown |
| `[UnityPlatform]` | Platform-specific test |

## EditMode Test Patterns

### Pure C# Logic Test

```csharp
using NUnit.Framework;

[TestFixture]
public class InventoryServiceTests
{
    private InventoryService mInventoryService;

    [SetUp]
    public void SetUp()
    {
        mInventoryService = new InventoryService();
    }

    [Test]
    public void AddItem_WhenSlotAvailable_ReturnsTrue()
    {
        // Arrange
        var item = new Item { Id = "sword", Count = 1 };

        // Act
        bool result = mInventoryService.AddItem(item);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, mInventoryService.ItemCount);
    }

    [Test]
    public void AddItem_WhenInventoryFull_ReturnsFalse()
    {
        // Arrange
        for (int i = 0; i < InventoryService.MAX_SLOTS; i++)
        {
            mInventoryService.AddItem(new Item { Id = $"item{i}" });
        }

        // Act
        bool result = mInventoryService.AddItem(new Item { Id = "overflow" });

        // Assert
        Assert.IsFalse(result);
    }
}
```

### ScriptableObject Test

```csharp
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class GameConfigTests
{
    private GameConfig mConfig;

    [SetUp]
    public void SetUp()
    {
        mConfig = ScriptableObject.CreateInstance<GameConfig>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(mConfig);
    }

    [Test]
    public void DefaultValues_AreValid()
    {
        Assert.Greater(mConfig.MaxHealth, 0);
        Assert.Greater(mConfig.StartingGold, 0);
    }

    [Test]
    public void Validate_WithInvalidHealth_ReturnsFalse()
    {
        mConfig.MaxHealth = -1;
        Assert.IsFalse(mConfig.Validate());
    }
}
```

### Parameterized Tests

```csharp
[TestFixture]
public class DamageCalculatorTests
{
    [TestCase(100, 10, 0, 90)]      // No armor
    [TestCase(100, 10, 5, 95)]      // With armor
    [TestCase(100, 100, 0, 0)]      // Lethal damage
    [TestCase(100, 1000, 0, 0)]     // Overkill (clamped)
    public void CalculateDamage_ReturnsExpectedHealth(
        int health, int damage, int armor, int expected)
    {
        int result = DamageCalculator.Calculate(health, damage, armor);
        Assert.AreEqual(expected, result);
    }

    private static IEnumerable<TestCaseData> EdgeCases
    {
        get
        {
            yield return new TestCaseData(0, 10, 0).Returns(0).SetName("Zero health");
            yield return new TestCaseData(100, 0, 0).Returns(100).SetName("Zero damage");
        }
    }

    [TestCaseSource(nameof(EdgeCases))]
    public int CalculateDamage_EdgeCases(int health, int damage, int armor)
    {
        return DamageCalculator.Calculate(health, damage, armor);
    }
}
```

## PlayMode Test Patterns

### MonoBehaviour Test

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

[TestFixture]
public class PlayerControllerTests
{
    private GameObject mPlayerObject;
    private PlayerController mPlayer;

    [SetUp]
    public void SetUp()
    {
        mPlayerObject = new GameObject("Player");
        mPlayer = mPlayerObject.AddComponent<PlayerController>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(mPlayerObject);
    }

    [UnityTest]
    public IEnumerator Move_WhenInputReceived_ChangesPosition()
    {
        // Arrange
        Vector3 startPosition = mPlayer.transform.position;

        // Act
        mPlayer.Move(Vector3.right);
        yield return null; // Wait one frame

        // Assert
        Assert.AreNotEqual(startPosition, mPlayer.transform.position);
    }

    [UnityTest]
    public IEnumerator TakeDamage_WhenHealthZero_TriggersDeathEvent()
    {
        // Arrange
        bool deathEventFired = false;
        mPlayer.OnDeath += () => deathEventFired = true;

        // Act
        mPlayer.TakeDamage(mPlayer.MaxHealth);
        yield return null;

        // Assert
        Assert.IsTrue(deathEventFired);
    }
}
```

### Scene Loading Test

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[TestFixture]
public class SceneTests
{
    [UnityTest]
    public IEnumerator MainMenu_LoadsWithoutErrors()
    {
        // Act
        yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);

        // Assert
        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);

        var canvas = Object.FindAnyObjectByType<Canvas>();
        Assert.IsNotNull(canvas, "MainMenu should have a Canvas");
    }

    [UnityTest]
    public IEnumerator GameScene_SpawnsPlayer()
    {
        // Act
        yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        yield return null; // Wait for Awake/Start

        // Assert
        var player = Object.FindAnyObjectByType<PlayerController>();
        Assert.IsNotNull(player, "GameScene should spawn a player");
    }
}
```

### Time-Based Tests

```csharp
[TestFixture]
public class CooldownTests
{
    [UnityTest]
    public IEnumerator Cooldown_AfterDuration_AllowsAction()
    {
        // Arrange
        var ability = new Ability { CooldownDuration = 0.5f };
        ability.Use();

        // Act
        yield return new WaitForSeconds(0.6f);

        // Assert
        Assert.IsTrue(ability.IsReady);
    }

    [UnityTest]
    public IEnumerator Cooldown_BeforeDuration_BlocksAction()
    {
        // Arrange
        var ability = new Ability { CooldownDuration = 1f };
        ability.Use();

        // Act
        yield return new WaitForSeconds(0.5f);

        // Assert
        Assert.IsFalse(ability.IsReady);
    }
}
```

## Async Test Patterns

### UniTask-Based Tests

```csharp
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class AsyncServiceTests
{
    [Test]
    public async Task LoadData_ReturnsValidResult()
    {
        // Arrange
        var service = new DataService();

        // Act
        var result = await service.LoadDataAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.Greater(result.Items.Count, 0);
    }

    [Test]
    public async Task LoadData_WithTimeout_ThrowsException()
    {
        // Arrange
        var service = new DataService { SimulateDelay = TimeSpan.FromSeconds(5) };
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await service.LoadDataAsync(cts.Token);
        });
    }
}
```

### IEnumerator to UniTask Migration

```csharp
// Before: Coroutine-based
[UnityTest]
public IEnumerator OldStyle_CoroutineTest()
{
    yield return new WaitForSeconds(1f);
    Assert.Pass();
}

// After: UniTask-based
[Test]
public async Task NewStyle_UniTaskTest()
{
    await UniTask.Delay(TimeSpan.FromSeconds(1));
    Assert.Pass();
}
```

## Test Fixture Configuration

### Assembly Definition (asmdef)

```json
{
    "name": "Game.Domain.Tests",
    "rootNamespace": "Game.Domain.Tests",
    "references": [
        "Game.Domain",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### Folder Structure

```
Assets/
  Scripts/
    Domain/
      Game.Domain.asmdef
      PlayerService.cs
    Tests/
      EditMode/
        Game.Domain.Tests.asmdef
        PlayerServiceTests.cs
      PlayMode/
        Game.PlayMode.Tests.asmdef
        PlayerControllerTests.cs
```

## Mocking Strategies

### Interface-Based Mocking

```csharp
// Production code
public interface ITimeProvider
{
    float DeltaTime { get; }
    float Time { get; }
}

public class UnityTimeProvider : ITimeProvider
{
    public float DeltaTime => UnityEngine.Time.deltaTime;
    public float Time => UnityEngine.Time.time;
}

// Test mock
public class MockTimeProvider : ITimeProvider
{
    public float DeltaTime { get; set; } = 0.016f;
    public float Time { get; set; } = 0f;

    public void AdvanceTime(float seconds)
    {
        Time += seconds;
    }
}

// Usage in tests
[TestFixture]
public class TimerTests
{
    [Test]
    public void Timer_AfterElapsed_Triggers()
    {
        // Arrange
        var mockTime = new MockTimeProvider();
        var timer = new Timer(mockTime, duration: 1f);

        // Act
        mockTime.AdvanceTime(1.1f);
        timer.Update();

        // Assert
        Assert.IsTrue(timer.HasElapsed);
    }
}
```

### VContainer DI Mocking

```csharp
using NUnit.Framework;
using VContainer;

[TestFixture]
public class ServiceWithDITests
{
    private IObjectResolver mContainer;

    [SetUp]
    public void SetUp()
    {
        var builder = new ContainerBuilder();

        // Register mocks
        builder.Register<ITimeProvider, MockTimeProvider>(Lifetime.Singleton);
        builder.Register<ILogger, MockLogger>(Lifetime.Singleton);

        // Register system under test
        builder.Register<GameService>(Lifetime.Transient);

        mContainer = builder.Build();
    }

    [TearDown]
    public void TearDown()
    {
        mContainer.Dispose();
    }

    [Test]
    public void GameService_UsesInjectedDependencies()
    {
        // Arrange
        var service = mContainer.Resolve<GameService>();

        // Act
        service.Initialize();

        // Assert
        var logger = mContainer.Resolve<ILogger>() as MockLogger;
        Assert.Contains("Initialized", logger.Messages);
    }
}
```

## Assertion Patterns

### Common Assertions

```csharp
// Equality
Assert.AreEqual(expected, actual);
Assert.AreNotEqual(unexpected, actual);

// Boolean
Assert.IsTrue(condition);
Assert.IsFalse(condition);

// Null checks
Assert.IsNull(obj);
Assert.IsNotNull(obj);

// Collections
Assert.Contains(item, collection);
Assert.IsEmpty(collection);
Assert.That(collection, Has.Count.EqualTo(5));

// Exceptions
Assert.Throws<ArgumentException>(() => Method());
Assert.DoesNotThrow(() => Method());

// Floating point (with tolerance)
Assert.AreEqual(1.5f, actual, 0.001f);

// String
Assert.That(str, Does.StartWith("prefix"));
Assert.That(str, Does.Contain("substring"));
```

### Custom Assertions

```csharp
public static class GameAssert
{
    public static void IsAlive(Entity entity, string message = null)
    {
        Assert.IsNotNull(entity, message ?? "Entity is null");
        Assert.Greater(entity.Health, 0, message ?? "Entity is dead");
    }

    public static void IsWithinBounds(Vector3 position, Bounds bounds)
    {
        Assert.IsTrue(bounds.Contains(position),
            $"Position {position} is outside bounds {bounds}");
    }
}

// Usage
[Test]
public void Player_AfterSpawn_IsAlive()
{
    var player = new Player();
    player.Spawn();
    GameAssert.IsAlive(player);
}
```

## Test Categories

### Defining Categories

```csharp
[TestFixture]
[Category("Unit")]
public class UnitTests
{
    [Test]
    [Category("Fast")]
    public void FastTest() { }

    [Test]
    [Category("Slow")]
    public void SlowTest() { }
}

[TestFixture]
[Category("Integration")]
public class IntegrationTests
{
    [Test]
    public void DatabaseTest() { }
}
```

### Running by Category

```powershell
# Run only unit tests
-testCategory "Unit"

# Run fast tests only
-testCategory "Fast"

# Exclude slow tests
-testCategory "!Slow"
```

## Best Practices

1. **AAA Pattern**: Arrange, Act, Assert - clear test structure
2. **One Assertion Focus**: Each test verifies one behavior
3. **Descriptive Names**: `MethodName_Condition_ExpectedResult`
4. **Fast Tests**: EditMode tests should run in milliseconds
5. **Isolated Tests**: No shared state between tests
6. **No Test Logic**: Avoid conditionals in tests
7. **Minimal Setup**: Only create what's needed for the test
8. **Clean Teardown**: Destroy all created objects
