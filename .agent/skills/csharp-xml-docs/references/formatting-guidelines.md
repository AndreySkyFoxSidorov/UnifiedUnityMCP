# Formatting Guidelines (POCU)

Guide for using various XML documentation tags effectively following POCU coding standards.

## When to Use `<br/>` vs `<list>`

### Use `<br/>` for Compact Line-Separated Content

**Best for:**
- Sequential steps (3+ steps)
- Feature lists (3+ items)
- Compact line-separated content
- Bilingual documentation with clear section breaks

**Example: Numbered Steps (POCU)**
```csharp
public class EventExecutor
{
    private readonly IEventRepository mRepository;

    /// <summary>
    /// Executes event
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Execution Process:</strong><br/>
    /// 1. Event definition lookup<br/>
    /// 2. Event-level condition evaluation<br/>
    /// 3. Direct action list processing<br/>
    /// 4. Execution time and metadata configuration
    /// </para>
    /// </remarks>
    public async UniTask<EventActionResult> ExecuteEvent(
        string systemId, string eventId, object contextDataOrNull = null)
    {
        Debug.Assert(systemId != null);
        Debug.Assert(eventId != null);

        // Implementation...
    }
}
```

**Example: Bulleted Lists (POCU)**
```csharp
/// <summary>
/// Action execution engine
/// </summary>
/// <remarks>
/// <para>
/// <strong>Key Features:</strong><br/>
/// - Thread-safe action handler management<br/>
/// - Event definition caching and updates<br/>
/// - Condition-based action filtering<br/>
/// - Performance measurement and result aggregation
/// </para>
/// </remarks>
public class ActionExecutionEngine
{
    private readonly Dictionary<string, IActionHandler> mHandlers;
    private readonly object mLock;

    // Implementation...
}
```

### Use `<list type="table">` for Structured Data

**Best for:**
- Key-value pairs
- Thresholds and structured reference data
- Data that benefits from tabular presentation

**Example (POCU):**
```csharp
public class PerformanceAnalyzer
{
    private readonly TimeSpan mExecutionTime;

    /// <summary>
    /// Returns performance grade based on execution time
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <item><term>Fast</term><description>&lt; 10ms</description></item>
    /// <item><term>Normal</term><description>10-50ms</description></item>
    /// <item><term>Slow</term><description>50-200ms</description></item>
    /// <item><term>Critical</term><description>&gt;= 200ms</description></item>
    /// </list>
    /// </remarks>
    public string GetPerformanceGrade()
    {
        double ms = mExecutionTime.TotalMilliseconds;
        return ms switch
        {
            < 10 => "Fast",
            < 50 => "Normal",
            < 200 => "Slow",
            _ => "Critical",
        };
    }
}
```

### Comparison Table

| Aspect | `<br/>` | `<list>` |
|--------|---------|----------|
| **Compactness** | Single-spaced lines | Extra spacing between items |
| **Markup Overhead** | Minimal (1 tag per line) | Verbose (`<item><description>`) |
| **IDE Compatibility** | VS 2019+, Rider, VS Code | Rider shows bullets instead of numbers |
| **Bilingual Structure** | Clean Korean/English separation | More complex nesting |
| **Recommended For** | Unity projects, internal docs | API documentation generators |

### Decision Guide

**Use `<br/>` when:**
- Methods have 3+ sequential steps
- Feature lists with 3+ items
- Any remarks section needing visual line separation
- Bilingual documentation with clear section breaks
- Compact display is preferred

**Use `<list type="table">` when:**
- Data is naturally key-value paired
- Thresholds or configuration values need documentation
- Tabular presentation adds clarity
- Generating external API documentation

**Use `<list type="number">` or `<list type="bullet">` when:**
- Your team prefers the XML format over `<br/>`
- Generating documentation with tools that format lists specially
- External API documentation standards require it

---

## Using `<value>` Tag for Properties

### When to Use `<value>`

Use `<value>` only when getter/setter have **side effects** or **non-obvious behavior**.

**Example: Property with Side Effects (POCU)**
```csharp
public class AnimationController
{
    private IAnimationSystem mCurrentSystem;

    /// <summary>
    /// Current animation mode
    /// </summary>
    /// <value>
    /// Gets or sets the animation mode. Setting this value automatically switches
    /// the underlying animation system. Returns null if no system is active.
    /// </value>
    public EAnimationMode? CurrentMode
    {
        get
        {
            if (mCurrentSystem != null)
            {
                return mCurrentSystem.Mode;
            }
            return null;
        }
        set
        {
            if (value.HasValue)
            {
                activateAnimationSystem(value.Value).Forget();
            }
        }
    }

    private async UniTask activateAnimationSystem(EAnimationMode mode)
    {
        // Implementation...
    }
}
```

### When NOT to Use `<value>`

**Simple Properties (No `<value>` needed):**
```csharp
public class ModelState
{
    private readonly List<Animation> mAnimations;

    /// <summary>
    /// Indicates whether the VRM model is loaded
    /// </summary>
    public bool IsModelLoaded { get; private set; }

    /// <summary>
    /// Number of available animations
    /// </summary>
    public int AnimationCount => mAnimations?.Count ?? 0;

    /// <summary>
    /// User's display name
    /// </summary>
    public string DisplayName { get; set; }
}
```

**Rationale:**
- Auto-properties with obvious behavior don't need `<value>`
- Simple computed properties only need `<summary>`
- `<value>` adds verbosity without benefit for straightforward cases

---

## Using `<exception>` Tag

### When to Document Exceptions

Document exceptions that are **part of the method's contract** - exceptions that callers should be aware of and might need to handle.

**Example (POCU):**
```csharp
public class VRMLoader
{
    private bool mbIsReady;

    /// <summary>
    /// Loads VRM model from specified path
    /// </summary>
    /// <param name="path">Addressable path to VRM asset</param>
    /// <returns>Loaded VRM instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when VRM context is not ready</exception>
    /// <exception cref="System.IO.FileNotFoundException">Thrown when VRM asset is not found</exception>
    public async UniTask<VrmInstance> LoadVRM(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (!mbIsReady)
        {
            throw new InvalidOperationException("VRM context is not ready");
        }

        // Implementation that might throw FileNotFoundException
    }
}
```

### When NOT to Document Exceptions

**Don't document:**
- Internal implementation exceptions not part of the contract
- Exceptions that are re-thrown as different types
- General system exceptions (OutOfMemoryException, StackOverflowException)

**Example of Over-Documentation (Avoid):**
```csharp
// ❌ Too verbose - documenting internal implementation details
/// <exception cref="NullReferenceException">Internal collection error</exception>
/// <exception cref="IndexOutOfRangeException">Internal array access error</exception>
/// <exception cref="OutOfMemoryException">System out of memory</exception>
```

---

## Using `<para>` for Multi-Paragraph Remarks

### When to Use `<para>`

Use `<para>` to create **visual separation** between different topics in `<remarks>`.

**Example (POCU):**
```csharp
public class EventExecutor
{
    private readonly IEventRepository mRepository;
    private readonly Dictionary<string, EventDefinition> mCache;

    /// <summary>
    /// Executes event with advanced options
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Execution Process:</strong><br/>
    /// 1. Event definition lookup<br/>
    /// 2. Condition evaluation<br/>
    /// 3. Action processing
    /// </para>
    /// <para>
    /// <strong>Performance Considerations:</strong><br/>
    /// - Event definitions are cached for repeated executions<br/>
    /// - Condition evaluation is optimized at event-level
    /// </para>
    /// <para>
    /// <strong>Error Handling:</strong><br/>
    /// Individual action failures don't stop execution unless configured otherwise.
    /// </para>
    /// </remarks>
    public async UniTask<EventActionResult> ExecuteEvent(
        string systemId, string eventId)
    {
        Debug.Assert(systemId != null);
        Debug.Assert(eventId != null);

        // Implementation...
    }
}
```

**Rationale**: `<para>` creates logical grouping and improves readability in complex remarks.

---

## Using `<code>` for Code Examples

### Inline Code Examples (POCU)

```csharp
public class PathParser
{
    /// <summary>
    /// Parses animation path
    /// </summary>
    /// <remarks>
    /// Supports two formats:
    /// <code>
    /// "VRMA/animation_name"     // VRM Animation
    /// "State/Layer/StateName"   // Animator Controller
    /// </code>
    /// </remarks>
    public AnimationPathInfo ParsePath(string path)
    {
        Debug.Assert(path != null);

        // Implementation...
    }
}
```

### Multi-Line Code Examples (POCU)

```csharp
/// <summary>
/// Action handler registration
/// </summary>
/// <remarks>
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// ActionExecutionEngine engine = new ActionExecutionEngine();
/// engine.RegisterHandler("PlayerPrefs", new PlayerPrefsActionHandler());
/// engine.RegisterHandler("Scene", new SceneActionHandler());
///
/// EventActionResult result = await engine.Execute(eventAction);
/// </code>
/// </para>
/// </remarks>
public class ActionExecutionEngine
{
    private readonly Dictionary<string, IActionHandler> mHandlers;

    // Implementation...
}
```

**Rationale**: Code examples in documentation help developers understand usage patterns quickly. Use explicit types, not var.

---

## Using `<see>` and `<seealso>` for Cross-References

### `<see>` for Inline References

```csharp
public class EventExecutor
{
    /// <summary>
    /// Executes event and returns detailed result
    /// </summary>
    /// <returns>
    /// Returns <see cref="EventActionResult"/> containing execution details
    /// </returns>
    public async UniTask<EventActionResult> ExecuteEvent(string eventId)
    {
        Debug.Assert(eventId != null);

        // Implementation...
    }
}
```

### `<seealso>` for Related Items

```csharp
/// <summary>
/// Individual action execution result
/// </summary>
/// <remarks>
/// Contains result information for a single action execution.
/// </remarks>
/// <seealso cref="EventActionResult"/>
/// <seealso cref="ActionExecutionEngine"/>
public class ActionResult
{
    private Dictionary<string, object> mOutputData;

    // Implementation...
}
```

**Rationale**: Cross-references help developers navigate related types and methods.

---

## Formatting for Bilingual Documentation

### Pattern 1: Separate Paragraphs

```csharp
/// <summary>
/// 액션 실행 엔진
/// </summary>
/// <remarks>
/// <para>
/// <strong>한글:</strong><br/>
/// 이벤트와 액션을 실행하는 핵심 엔진입니다.<br/>
/// 조건 평가, 액션 처리, 결과 집계를 담당합니다.
/// </para>
/// <para>
/// <strong>English:</strong><br/>
/// Core engine for executing events and actions.<br/>
/// Handles condition evaluation, action processing, and result aggregation.
/// </para>
/// </remarks>
public class ActionExecutionEngine
{
    private readonly Dictionary<string, IActionHandler> mHandlers;

    // Implementation...
}
```

### Pattern 2: Side-by-Side

```csharp
/// <summary>
/// 액션이 건너뛰어졌는지 여부 / Indicates whether action was skipped
/// </summary>
/// <remarks>
/// 조건 미충족 시 true / Set to true when conditions are not met<br/>
/// Success와 독립적 / Independent of Success value
/// </remarks>
public bool Skipped { get; set; }
```

### Pattern 3: Primary Language + Translation

```csharp
public class EventExecutor
{
    /// <summary>
    /// Executes event asynchronously
    /// </summary>
    /// <remarks>
    /// <para>
    /// Processes event definition, evaluates conditions, and executes actions.
    /// </para>
    /// <para>
    /// <strong>한글 설명:</strong> 이벤트 정의를 처리하고, 조건을 평가한 후, 액션을 실행합니다.
    /// </para>
    /// </remarks>
    public async UniTask<EventActionResult> ExecuteEvent(string eventId)
    {
        Debug.Assert(eventId != null);

        // Implementation...
    }
}
```

**Recommendation**: Choose one pattern and use it consistently within a file or project.

---

## Summary

### Tag Usage Quick Reference

| Tag | Purpose | When to Use |
|-----|---------|-------------|
| `<summary>` | Brief description | Always for public members |
| `<remarks>` | Detailed explanation | Complex concepts, special behaviors |
| `<param>` | Parameter description | Methods with parameters |
| `<returns>` | Return value description | Methods that return values |
| `<exception>` | Exception documentation | Exceptions part of method contract |
| `<value>` | Property behavior | Properties with side effects |
| `<br/>` | Line break | Sequential steps, compact lists |
| `<list>` | Structured list | Key-value pairs, tables |
| `<para>` | Paragraph separation | Multi-topic remarks |
| `<code>` | Code examples | Usage examples |
| `<see>` | Inline reference | Cross-reference types/methods |
| `<seealso>` | Related items | Related types/methods |
| `<inheritdoc/>` | Inherit documentation | Implementation classes |

### Best Practices (POCU)

1. **Keep it simple**: Use minimal tags for straightforward cases
2. **Add structure**: Use `<para>`, `<br/>`, `<list>` for complex remarks
3. **Be consistent**: Choose formatting patterns and stick to them
4. **Think readability**: Optimize for IntelliSense display
5. **Document contracts**: Focus on public API contracts, not implementation details
6. **POCU Naming**: Use mPascalCase for private fields, camelCase for private methods
7. **No Async Suffix**: Method names don't need Async suffix per POCU
8. **Explicit Types**: Use explicit types in code examples, not var
