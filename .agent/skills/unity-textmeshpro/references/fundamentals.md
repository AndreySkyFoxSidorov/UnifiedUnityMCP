# TextMeshPro Fundamentals

## Table of Contents
- [SDF Technology](#sdf-technology)
- [Font Asset Creation](#font-asset-creation)
- [Character Sets and Fallback](#character-sets-and-fallback)
- [Sprite Assets](#sprite-assets)
- [Style Sheets](#style-sheets)

## SDF Technology

### What is SDF (Signed Distance Field)?

SDF stores distance-to-edge information instead of pixel colors:
- **Positive values**: Inside the glyph
- **Negative values**: Outside the glyph
- **Zero**: On the edge

### Benefits

```
Traditional Bitmap:
- Resolution dependent
- Blurry when scaled up
- Sharp edges at native size only

SDF Text:
- Resolution independent
- Crisp at any scale
- Enables outline, glow, shadow effects in shader
- Smaller texture memory for equivalent quality
```

### SDF Rendering Pipeline

```
Font File (.ttf/.otf)
    ↓
Font Asset Creator (generates SDF atlas)
    ↓
TMP_FontAsset (runtime asset)
    ↓
TextMeshPro Shader (renders SDF)
    ↓
Screen Output (crisp text at any size)
```

## Font Asset Creation

### Font Asset Creator Settings

Access: **Window > TextMeshPro > Font Asset Creator**

```yaml
Source Font File: .ttf or .otf font file

Sampling Point Size:
  - Higher = better quality
  - Auto Size: fits maximum characters
  - Recommendation: 90+ for high quality

Padding:
  - Space between glyphs in atlas
  - 5: Minimal effects
  - 7: Standard (outline, shadow)
  - 9+: Heavy effects (glow, thick outline)

Packing Method:
  - Fast: Quick generation
  - Optimum: Better atlas usage

Atlas Resolution:
  - 512x512: Small character sets
  - 1024x1024: Latin scripts
  - 2048x2048: Extended Latin + symbols
  - 4096x4096: CJK (Chinese/Japanese/Korean)

Character Set:
  - ASCII: Basic English (95 chars)
  - Extended ASCII: European languages
  - Custom Characters: Specific set
  - Unicode Range: Hex ranges
  - Custom Range: Multiple Unicode ranges

Render Mode:
  - SDFAA: Standard SDF (recommended)
  - SDFAA_HINTED: Hinted for small sizes
  - SDF: No anti-aliasing
```

### Static vs Dynamic Font Assets

```csharp
// Static Font Asset
// - Pre-generated at build time
// - Best runtime performance
// - Fixed character set
[SerializeField] private TMP_FontAsset staticFont;

// Dynamic Font Asset
// - Generates glyphs at runtime
// - Flexible character support
// - Initial performance cost
[SerializeField] private TMP_FontAsset dynamicFont;

// Check if character exists
if (!font.HasCharacter('A'))
{
    // Character not in font atlas
}

// Dynamic font: request character addition
font.TryAddCharacters("New characters to add");
```

### Font Asset Settings

```yaml
Face Info:
  Point Size: Sampling size used
  Scale: Rendering scale multiplier
  Line Height: Default line spacing

Generation Settings:
  Atlas Population Mode:
    - Static: No runtime generation
    - Dynamic: Runtime generation enabled
    - DynamicOS: Uses OS font as source

Atlas Render Mode: SDF mode used

Sampling Point Size: Generation size

Padding: Glyph spacing in atlas

Atlas Width/Height: Texture dimensions

Multi Atlas Textures: Enable for large sets
```

## Character Sets and Fallback

### Fallback Font Chain

```csharp
// Configure fallback in Font Asset inspector
// or programmatically:
TMP_FontAsset mainFont;
TMP_FontAsset fallbackFont;

// Add fallback
mainFont.fallbackFontAssetTable.Add(fallbackFont);

// Fallback chain order:
// 1. Primary font
// 2. Fallback fonts (in order)
// 3. Default font (TMP Settings)
// 4. Missing glyph placeholder
```

### Character Set Strategies

```
Strategy 1: Minimal Static
- Only characters used in game
- Smallest atlas size
- Generate custom list from localization files

Strategy 2: Language-Specific
- Separate font assets per language
- Load appropriate asset at runtime
- Optimal memory per language

Strategy 3: Dynamic Fallback
- Static primary (common chars)
- Dynamic fallback (rare chars)
- Balance performance and flexibility
```

### Custom Character List Generation

```csharp
// Extract unique characters from text files
public static string GetUniqueCharacters(string[] textFiles)
{
    var chars = new HashSet<char>();
    foreach (var file in textFiles)
    {
        var content = File.ReadAllText(file);
        foreach (var c in content)
        {
            if (!char.IsControl(c))
                chars.Add(c);
        }
    }
    return new string(chars.OrderBy(c => c).ToArray());
}
```

## Sprite Assets

### Creating Sprite Assets

1. Create sprite sheet texture
2. **Create > TextMeshPro > Sprite Asset**
3. Configure sprite definitions

```yaml
Sprite Asset Settings:
  Sprite Sheet: Source texture
  Sprites: List of sprite definitions
    - Name: Sprite identifier
    - Unicode: Optional Unicode value
    - Position/Size: Atlas coordinates
    - Offset/Advance: Positioning
```

### Using Sprites in Text

```csharp
// By index
text.text = "Health: <sprite=0>";

// By name
text.text = "Coins: <sprite name=\"coin\">";

// With tint
text.text = "<sprite=0 tint=1>";

// From specific asset
text.text = "<sprite=\"icons\" index=5>";
```

### Sprite Asset Configuration

```csharp
// Assign sprite asset to text
text.spriteAsset = mySpriteAsset;

// Multiple sprite assets via fallback
mainSpriteAsset.fallbackSpriteAssets.Add(additionalSprites);
```

## Style Sheets

### Creating Style Sheets

1. **Create > TextMeshPro > Style Sheet**
2. Define named styles

```yaml
Style Sheet Definition:
  Style Name: "title"
  Opening Tags: "<size=48><b><color=#FFD700>"
  Closing Tags: "</color></b></size>"
```

### Using Styles

```csharp
// Apply style in text
text.text = "<style=\"title\">Game Over</style>";

// Nested styles
text.text = "<style=\"warning\">Error: <style=\"code\">NullRef</style></style>";
```

### Style Sheet Best Practices

```csharp
// Define consistent styles
/*
Style: "heading"     -> Large, bold, primary color
Style: "body"        -> Normal size, regular
Style: "emphasis"    -> Italic, accent color
Style: "warning"     -> Red, bold
Style: "link"        -> Blue, underline
*/

// Assign default style sheet in TMP Settings
// or per-text component via inspector
```

### Programmatic Style Access

```csharp
// Get style from stylesheet
TMP_StyleSheet styleSheet = text.styleSheet;
TMP_Style style = styleSheet.GetStyle("title");

if (style != null)
{
    Debug.Log($"Opening: {style.styleOpeningDefinition}");
    Debug.Log($"Closing: {style.styleClosingDefinition}");
}
```

## Font Features

### OpenType Features

```csharp
// Enable font features
text.fontFeatures = FontFeatures.Kerning | FontFeatures.Ligatures;

// Individual control
text.enableKerning = true;
text.enableWordWrapping = true;
text.overflowMode = TextOverflowModes.Ellipsis;
```

### Text Alignment

```csharp
// Horizontal alignment
text.horizontalAlignment = HorizontalAlignmentOptions.Center;

// Vertical alignment
text.verticalAlignment = VerticalAlignmentOptions.Middle;

// Combined (via inspector or)
text.alignment = TextAlignmentOptions.Center;
```

### Text Geometry Options

```csharp
// Character/word/line spacing
text.characterSpacing = 5f;
text.wordSpacing = 10f;
text.lineSpacing = 1.5f;
text.paragraphSpacing = 20f;

// Margins
text.margin = new Vector4(10, 10, 10, 10); // left, top, right, bottom
```
