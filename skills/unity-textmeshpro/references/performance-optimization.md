# TextMeshPro Performance Optimization

## Table of Contents
- [Mesh Geometry Optimization](#mesh-geometry-optimization)
- [Dynamic Batching](#dynamic-batching)
- [Font Atlas Management](#font-atlas-management)
- [Text Update Patterns](#text-update-patterns)
- [Profiling and Debugging](#profiling-and-debugging)

## Mesh Geometry Optimization

### Geometry Mode Selection

```csharp
// Text Geometry Mode options
text.geometrySortingOrder = GeometrySortingOrder.Normal;

// For overlapping text, use Reverse for proper layering
text.geometrySortingOrder = GeometrySortingOrder.Reverse;
```

### Rich Text Impact

```
Geometry Cost by Feature:
├── Plain text: 4 vertices per character
├── Underline: +4 vertices per underlined segment
├── Strikethrough: +4 vertices per segment
├── Highlight/Mark: +4 vertices per segment
└── Inline sprites: +4 vertices per sprite

Optimization:
- Minimize underline/strikethrough use in dynamic text
- Use highlight sparingly on frequently updated text
- Pre-render complex decorated text as sprite when static
```

### Vertex Count Reduction

```csharp
// Check vertex count
int vertexCount = text.textInfo.characterCount * 4;
Debug.Log($"Vertices: {vertexCount}");

// Reduce by:
// 1. Shorter text
// 2. Fewer rich text features
// 3. Smaller visible character count

// Extra Settings (Inspector)
// - Disable "Extra Padding" when not using effects
// - Use "Truncate" overflow for fixed-size containers
```

## Dynamic Batching

### Material Instance Management

```csharp
// WRONG: Creates material instance per text
text.fontMaterial.SetColor("_FaceColor", Color.red); // New instance

// CORRECT: Use shared materials
[SerializeField] private Material sharedRedMaterial;
text.fontMaterial = sharedRedMaterial; // Shares material

// CORRECT: Modify base material (affects all users)
text.font.material.SetColor("_FaceColor", Color.red);
```

### Batching Requirements

```
For texts to batch together:
1. Same font asset
2. Same material (shared, not instanced)
3. Same texture atlas
4. Same render queue
5. No material property blocks

Check batching:
- Frame Debugger (Window > Analysis > Frame Debugger)
- Look for "Draw Dynamic" calls grouping
```

### Canvas Optimization (UGUI)

```csharp
// Separate canvases for static vs dynamic text
/*
StaticTextCanvas (rarely rebuilds)
├── Title
├── Labels
└── Instructions

DynamicTextCanvas (frequent rebuilds)
├── Score
├── Timer
└── Messages
*/

// Disable raycast on non-interactive text
text.raycastTarget = false;
```

### Batching Strategies

```csharp
// Strategy 1: Shared Material Variants
// Create material presets for different styles
// Assign same preset to multiple texts

// Strategy 2: Font Asset Consolidation
// Use single font with multiple weights as fallbacks
// Instead of separate font assets per weight

// Strategy 3: Sprite Atlas Integration
// Include sprites in same atlas as font
// Enables batching text + sprites
```

## Font Atlas Management

### Atlas Memory Calculation

```
Memory = Width × Height × BytesPerPixel

Examples:
- 1024×1024 SDF (1 channel): ~1 MB
- 2048×2048 SDF (1 channel): ~4 MB
- 4096×4096 SDF (1 channel): ~16 MB

Multi-Atlas:
- Enabled: Multiple smaller textures
- Trade-off: More draw calls vs memory flexibility
```

### Dynamic Atlas Growth

```csharp
// Dynamic font atlas behavior
// New characters → Atlas regeneration → Memory spike

// Monitor dynamic atlas
TMP_FontAsset font = text.font;
if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
{
    var atlasTexture = font.atlasTexture;
    Debug.Log($"Atlas size: {atlasTexture.width}x{atlasTexture.height}");
}

// Clear dynamic atlas (use sparingly)
font.ClearFontAssetData();
```

### Memory Optimization Strategies

```csharp
// 1. Pre-warm dynamic fonts
void PrewarmFont(TMP_FontAsset font, string characters)
{
    font.TryAddCharacters(characters);
}

// 2. Use appropriate atlas sizes
// Mobile: 1024x1024 max per font
// Desktop: 2048x2048 acceptable
// CJK: Consider multi-atlas

// 3. Unload unused font assets
Resources.UnloadAsset(unusedFont);

// 4. Share font assets across scenes
// Use Addressables for async loading
```

## Text Update Patterns

### Change Detection

```csharp
public class OptimizedScoreDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    private int _cachedScore = -1;

    public void UpdateScore(int newScore)
    {
        if (newScore == _cachedScore) return;

        _cachedScore = newScore;
        scoreText.SetText("Score: {0}", newScore);
    }
}
```

### SetText vs text Property

```csharp
// BENCHMARK COMPARISON (approximate, varies by content)

// text property with string interpolation
text.text = $"HP: {hp}/{maxHp}";
// - String allocation
// - Full text parsing
// - ~0.5-1.0ms for complex text

// SetText with parameters
text.SetText("HP: {0}/{1}", hp, maxHp);
// - Minimal allocation
// - Optimized parsing
// - ~0.2-0.4ms for same text

// SetText with StringBuilder
private StringBuilder _sb = new StringBuilder(64);
_sb.Clear().Append("HP: ").Append(hp).Append("/").Append(maxHp);
text.SetText(_sb);
// - Zero allocation after warmup
// - ~0.1-0.3ms
```

### Batch Update Pattern

```csharp
public class TextUpdateBatcher : MonoBehaviour
{
    private readonly List<(TMP_Text text, string content)> _pendingUpdates = new();
    private bool _updateScheduled;

    public void QueueUpdate(TMP_Text text, string content)
    {
        _pendingUpdates.Add((text, content));

        if (!_updateScheduled)
        {
            _updateScheduled = true;
            StartCoroutine(ProcessUpdates());
        }
    }

    private IEnumerator ProcessUpdates()
    {
        yield return new WaitForEndOfFrame();

        foreach (var (text, content) in _pendingUpdates)
        {
            text.text = content;
        }

        _pendingUpdates.Clear();
        _updateScheduled = false;
    }
}
```

### Object Pooling for Dynamic Text

```csharp
public class TextPool : MonoBehaviour
{
    [SerializeField] private TMP_Text prefab;
    [SerializeField] private Transform container;

    private readonly Queue<TMP_Text> _pool = new();

    public TMP_Text Get()
    {
        if (_pool.Count > 0)
        {
            var text = _pool.Dequeue();
            text.gameObject.SetActive(true);
            return text;
        }
        return Instantiate(prefab, container);
    }

    public void Return(TMP_Text text)
    {
        text.text = string.Empty;
        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }
}
```

## Profiling and Debugging

### Unity Profiler Markers

```
Key markers to watch:
├── TMP.TextMeshPro.GenerateText
├── TMP.TextMeshPro.SetText
├── TMP.FontAsset.TryAddCharacters
├── Canvas.BuildBatch
└── Canvas.SendWillRenderCanvases

High cost indicators:
- GenerateText: Complex text or frequent updates
- TryAddCharacters: Dynamic font atlas expansion
- BuildBatch: Canvas rebuild (check text isolation)
```

### Frame Debugger Analysis

```
Window > Analysis > Frame Debugger

Look for:
1. Draw calls per text element
2. Material batching groups
3. Overdraw from text effects
4. Separate passes for outlines/shadows

Optimization targets:
- Multiple texts → single draw call
- Minimize separate material passes
```

### TMP Debug Visualization

```csharp
// Enable in inspector or via code
text.enableWordWrapping = true;

// Debug bounds
var bounds = text.textBounds;
Debug.Log($"Text bounds: {bounds.size}");

// Character info
TMP_TextInfo textInfo = text.textInfo;
for (int i = 0; i < textInfo.characterCount; i++)
{
    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
    Debug.Log($"Char '{charInfo.character}': {charInfo.vertex_BL.position}");
}
```

### Memory Profiler Usage

```csharp
// Track font asset memory
#if UNITY_EDITOR
void LogFontMemory(TMP_FontAsset font)
{
    var atlasTexture = font.atlasTexture;
    long memory = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(atlasTexture);
    Debug.Log($"Font '{font.name}' atlas: {memory / 1024f:F1} KB");
}
#endif
```

### Common Performance Issues

```yaml
Issue: High GC allocation
Cause: Using text property with string concatenation
Fix: Use SetText() with parameters or StringBuilder

Issue: Frequent mesh rebuilds
Cause: Updating text every frame
Fix: Cache values, update only on change

Issue: Large memory footprint
Cause: Oversized font atlas or too many font assets
Fix: Optimize atlas size, use fallback chain

Issue: Draw call explosion
Cause: Unique materials per text instance
Fix: Use shared materials, batch similar text

Issue: Slow initial text render
Cause: Dynamic font generating glyphs
Fix: Pre-warm fonts or use static font assets
```
