# TextMeshPro Advanced Patterns

## Table of Contents
- [Custom Shaders and Effects](#custom-shaders-and-effects)
- [Text Animation](#text-animation)
- [Localization Integration](#localization-integration)
- [Typewriter Effects](#typewriter-effects)
- [Link and Event Handling](#link-and-event-handling)

## Custom Shaders and Effects

### Material Property Access

```csharp
// Access shader properties via ShaderUtilities
Material mat = text.fontMaterial;

// Face (main text)
mat.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);

// Outline
mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);

// Underlay (shadow)
mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.5f));
mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1f);
mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1f);

// Glow
mat.SetColor(ShaderUtilities.ID_GlowColor, Color.cyan);
mat.SetFloat(ShaderUtilities.ID_GlowOffset, 0.5f);
mat.SetFloat(ShaderUtilities.ID_GlowPower, 0.5f);
```

### Creating Material Presets

```csharp
// Runtime material creation
public Material CreateOutlineMaterial(TMP_FontAsset font, Color outlineColor, float width)
{
    Material baseMaterial = font.material;
    Material outlineMat = new Material(baseMaterial);

    outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
    outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
    outlineMat.EnableKeyword("OUTLINE_ON");

    return outlineMat;
}

// Apply preset
text.fontMaterial = CreateOutlineMaterial(text.font, Color.black, 0.2f);
```

### Shader Keywords

```csharp
// Enable/disable shader features
Material mat = text.fontMaterial;

// Outline
mat.EnableKeyword("OUTLINE_ON");
mat.DisableKeyword("OUTLINE_ON");

// Underlay (shadow)
mat.EnableKeyword("UNDERLAY_ON");

// Glow
mat.EnableKeyword("GLOW_ON");

// Bevel
mat.EnableKeyword("BEVEL_ON");
```

### Custom Shader Development

```hlsl
// Basic structure for custom TMP shader modification
Shader "Custom/TMP_CustomEffect"
{
    Properties
    {
        // Include standard TMP properties
        _FaceColor("Face Color", Color) = (1,1,1,1)
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth("Outline Width", Range(0,1)) = 0

        // Custom properties
        _CustomEffect("Custom Effect", Range(0,1)) = 0
    }

    SubShader
    {
        // Inherit from TMP shader includes
        // Add custom effect in fragment shader
    }
}
```

## Text Animation

### Per-Character Animation

```csharp
public class WaveTextAnimation : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float waveSpeed = 2f;
    [SerializeField] private float waveHeight = 10f;

    private void Update()
    {
        text.ForceMeshUpdate();

        TMP_TextInfo textInfo = text.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            float offset = Mathf.Sin(Time.time * waveSpeed + i * 0.5f) * waveHeight;

            vertices[vertexIndex + 0].y += offset;
            vertices[vertexIndex + 1].y += offset;
            vertices[vertexIndex + 2].y += offset;
            vertices[vertexIndex + 3].y += offset;
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
```

### Color Gradient Animation

```csharp
public class RainbowTextAnimation : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float colorSpeed = 1f;

    private void Update()
    {
        text.ForceMeshUpdate();

        TMP_TextInfo textInfo = text.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            float hue = (Time.time * colorSpeed + i * 0.1f) % 1f;
            Color32 color = Color.HSVToRGB(hue, 1f, 1f);

            colors[vertexIndex + 0] = color;
            colors[vertexIndex + 1] = color;
            colors[vertexIndex + 2] = color;
            colors[vertexIndex + 3] = color;
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
```

### Scale/Rotation Animation

```csharp
public class PulseTextAnimation : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.2f;

    private void Update()
    {
        text.ForceMeshUpdate();

        TMP_TextInfo textInfo = text.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // Get character center
            Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2f;

            // Calculate scale
            float scale = 1f + Mathf.Sin(Time.time * pulseSpeed + i) * pulseAmount;

            // Apply scale from center
            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] = center + (vertices[vertexIndex + j] - center) * scale;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
```

## Localization Integration

### Unity Localization Package

```csharp
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

// Add LocalizeStringEvent component to GameObject
// Link to TMP_Text.text property

// Or programmatically:
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private LocalizedString localizedString;

    private void OnEnable()
    {
        localizedString.StringChanged += UpdateText;
    }

    private void OnDisable()
    {
        localizedString.StringChanged -= UpdateText;
    }

    private void UpdateText(string value)
    {
        text.text = value;
    }
}
```

### Font Fallback for Localization

```csharp
public class LocalizedFontManager : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset latinFont;
    [SerializeField] private TMP_FontAsset cjkFont;
    [SerializeField] private TMP_FontAsset arabicFont;

    public TMP_FontAsset GetFontForLocale(string localeCode)
    {
        return localeCode switch
        {
            "ja" or "ko" or "zh" => cjkFont,
            "ar" or "fa" or "he" => arabicFont,
            _ => latinFont
        };
    }

    public void ApplyLocaleFont(TMP_Text text, string localeCode)
    {
        text.font = GetFontForLocale(localeCode);
    }
}
```

### Dynamic Character Loading

```csharp
public class DynamicLocalizationLoader : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset dynamicFont;

    public async UniTask PreloadCharactersForLocale(string localeCode)
    {
        // Load localization strings
        var strings = await LoadLocalizationStrings(localeCode);

        // Extract unique characters
        var uniqueChars = new HashSet<char>();
        foreach (var str in strings)
        {
            foreach (var c in str)
            {
                if (!char.IsControl(c))
                    uniqueChars.Add(c);
            }
        }

        // Add to dynamic font
        string charSet = new string(uniqueChars.ToArray());
        dynamicFont.TryAddCharacters(charSet);
    }
}
```

## Typewriter Effects

### Basic Typewriter

```csharp
public class TypewriterEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float charactersPerSecond = 20f;

    private string _fullText;
    private float _timer;
    private int _currentCharIndex;

    public void StartTyping(string content)
    {
        _fullText = content;
        _currentCharIndex = 0;
        _timer = 0;
        text.text = "";
        text.maxVisibleCharacters = 0;
        text.text = _fullText; // Set full text but hide characters
    }

    private void Update()
    {
        if (_currentCharIndex >= _fullText.Length) return;

        _timer += Time.deltaTime;
        int targetChars = Mathf.FloorToInt(_timer * charactersPerSecond);

        if (targetChars > _currentCharIndex)
        {
            _currentCharIndex = Mathf.Min(targetChars, _fullText.Length);
            text.maxVisibleCharacters = _currentCharIndex;
        }
    }

    public void SkipToEnd()
    {
        _currentCharIndex = _fullText.Length;
        text.maxVisibleCharacters = _currentCharIndex;
    }
}
```

### Advanced Typewriter with Rich Text

```csharp
public class RichTypewriterEffect : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float baseDelay = 0.05f;

    public async UniTask TypeText(string content, CancellationToken ct = default)
    {
        text.text = content;
        text.ForceMeshUpdate();

        int totalVisible = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalVisible; i++)
        {
            ct.ThrowIfCancellationRequested();

            text.maxVisibleCharacters = i;

            // Variable delay based on punctuation
            float delay = baseDelay;
            if (i > 0 && i <= content.Length)
            {
                char c = content[i - 1];
                if (c == '.' || c == '!' || c == '?') delay *= 4f;
                else if (c == ',') delay *= 2f;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
        }
    }
}
```

### Typewriter with Sound Effects

```csharp
public class TypewriterWithSound : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] typeSounds;
    [SerializeField] private float pitch variance = 0.1f;

    private void PlayTypeSound()
    {
        if (typeSounds.Length == 0) return;

        audioSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        audioSource.PlayOneShot(typeSounds[Random.Range(0, typeSounds.Length)]);
    }
}
```

## Link and Event Handling

### Clickable Links

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class LinkHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text text;

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            text,
            eventData.position,
            eventData.pressEventCamera
        );

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();
            string linkText = linkInfo.GetLinkText();

            HandleLinkClick(linkId, linkText);
        }
    }

    private void HandleLinkClick(string linkId, string linkText)
    {
        Debug.Log($"Clicked link: {linkId} ({linkText})");

        if (linkId.StartsWith("http"))
        {
            Application.OpenURL(linkId);
        }
        else
        {
            // Custom link handling
            OnCustomLinkClicked(linkId);
        }
    }

    protected virtual void OnCustomLinkClicked(string linkId)
    {
        // Override in derived classes
    }
}
```

### Link Formatting in Text

```csharp
// Basic link
text.text = "Visit <link=\"https://unity.com\">Unity</link> website";

// Custom link IDs
text.text = "Open <link=\"inventory\">Inventory</link> or <link=\"shop\">Shop</link>";

// Styled links
text.text = "<link=\"help\"><color=#0066CC><u>Help</u></color></link>";
```

### Hover Effects

```csharp
public class LinkHoverHandler : MonoBehaviour, IPointerMoveHandler
{
    [SerializeField] private TMP_Text text;

    private int _lastHoveredLink = -1;

    public void OnPointerMove(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            text,
            eventData.position,
            eventData.pressEventCamera
        );

        if (linkIndex != _lastHoveredLink)
        {
            if (_lastHoveredLink != -1)
            {
                OnLinkExit(_lastHoveredLink);
            }

            if (linkIndex != -1)
            {
                OnLinkEnter(linkIndex);
            }

            _lastHoveredLink = linkIndex;
        }
    }

    private void OnLinkEnter(int linkIndex)
    {
        // Change cursor, highlight, etc.
        Cursor.SetCursor(handCursor, Vector2.zero, CursorMode.Auto);
    }

    private void OnLinkExit(int linkIndex)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
```

### Word/Character Click Detection

```csharp
public class WordClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TMP_Text text;

    public event System.Action<string> OnWordClicked;
    public event System.Action<char, int> OnCharacterClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Word detection
        int wordIndex = TMP_TextUtilities.FindIntersectingWord(
            text,
            eventData.position,
            eventData.pressEventCamera
        );

        if (wordIndex != -1)
        {
            TMP_WordInfo wordInfo = text.textInfo.wordInfo[wordIndex];
            string word = wordInfo.GetWord();
            OnWordClicked?.Invoke(word);
        }

        // Character detection
        int charIndex = TMP_TextUtilities.FindIntersectingCharacter(
            text,
            eventData.position,
            eventData.pressEventCamera,
            true
        );

        if (charIndex != -1)
        {
            char character = text.textInfo.characterInfo[charIndex].character;
            OnCharacterClicked?.Invoke(character, charIndex);
        }
    }
}
```

### Text Selection

```csharp
public class TextSelector : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] private TMP_Text text;

    private int _selectionStart = -1;
    private int _selectionEnd = -1;

    public void OnPointerDown(PointerEventData eventData)
    {
        int charIndex = GetCharacterIndex(eventData);
        _selectionStart = charIndex;
        _selectionEnd = charIndex;
    }

    public void OnDrag(PointerEventData eventData)
    {
        int charIndex = GetCharacterIndex(eventData);
        _selectionEnd = charIndex;
        HighlightSelection();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        string selectedText = GetSelectedText();
        Debug.Log($"Selected: {selectedText}");
    }

    private int GetCharacterIndex(PointerEventData eventData)
    {
        return TMP_TextUtilities.FindIntersectingCharacter(
            text,
            eventData.position,
            eventData.pressEventCamera,
            true
        );
    }

    private void HighlightSelection()
    {
        // Implement vertex color modification for selection highlight
    }

    private string GetSelectedText()
    {
        if (_selectionStart < 0 || _selectionEnd < 0) return "";

        int start = Mathf.Min(_selectionStart, _selectionEnd);
        int end = Mathf.Max(_selectionStart, _selectionEnd);

        var sb = new StringBuilder();
        for (int i = start; i <= end && i < text.textInfo.characterCount; i++)
        {
            sb.Append(text.textInfo.characterInfo[i].character);
        }
        return sb.ToString();
    }
}
```
