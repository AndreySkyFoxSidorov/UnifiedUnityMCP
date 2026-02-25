// Assets/mcp/Editor/McpToolbarToggle.cs

using System;
using UnityEditor;
using UnityEngine;
using Mcp.Editor;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Toolbars;
#endif

#if !UNITY_6000_0_OR_NEWER
using UnityEngine.UIElements;
#endif

[InitializeOnLoad]
public static class McpToolbarToggle
{
    private const string PrefKey = "SF_MCP_Running";
    private const string Label = "MCP";

#if UNITY_6000_0_OR_NEWER
    private const string ToolbarId = "SkyFox/MCP Toggle";
#endif

#if !UNITY_6000_0_OR_NEWER
    private const string ButtonName = "SF_MCP_Toggle_Button";
    private static bool _installed;
    private static Button _legacyButton;
#endif

    private static bool _isRunning;

    static McpToolbarToggle()
    {
        // By default, MCP should start
        if (!EditorPrefs.HasKey(PrefKey))
        {
            EditorPrefs.SetBool(PrefKey, true);
        }

        _isRunning = EditorPrefs.GetBool(PrefKey, true);

        if (_isRunning)
        {
            UnityMcpServer.Start();
        }

        // Hook up UpdateDispatcher
        EditorApplication.update -= UnityMcpServer.UpdateDispatcher;
        EditorApplication.update += UnityMcpServer.UpdateDispatcher;

        // Ensure clean shutdown
        EditorApplication.quitting -= UnityMcpServer.Stop;
        EditorApplication.quitting += UnityMcpServer.Stop;

#if !UNITY_6000_0_OR_NEWER
        EditorApplication.update -= TryInstallLegacyOnUpdate;
        EditorApplication.update += TryInstallLegacyOnUpdate;
#endif
    }

    public static bool IsRunning => _isRunning;

    public static void Toggle()
    {
        SetRunning(!_isRunning);
    }

    public static void SetRunning(bool value)
    {
        if (_isRunning == value) return;

        _isRunning = value;
        EditorPrefs.SetBool(PrefKey, _isRunning);

        if (_isRunning) UnityMcpServer.Start();
        else UnityMcpServer.Stop();

        RefreshVisual();
    }

    private static void RefreshVisual()
    {
#if UNITY_6000_0_OR_NEWER
        MainToolbar.Refresh(ToolbarId);
#else
        ApplyLegacyVisual();
#endif
    }

    private static string GetColoredLabel()
    {
        if (_isRunning)
            return $"<color=#FF3333><b>{Label}</b></color>"; // Red when running
        return $"<color=#33FF33>{Label}</color>"; // Green when off
    }

#if UNITY_6000_0_OR_NEWER

    [MainToolbarElement(ToolbarId, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 200)]
    public static MainToolbarElement Create()
    {
        var content = new MainToolbarContent(GetColoredLabel(), "Toggle MCP (Start/Stop)");
        return new MainToolbarButton(content, Toggle);
    }

#else

    private static void TryInstallLegacyOnUpdate()
    {
        if (_installed)
        {
            EditorApplication.update -= TryInstallLegacyOnUpdate;
            return;
        }

        if (TryInstallLegacy())
        {
            _installed = true;
            EditorApplication.update -= TryInstallLegacyOnUpdate;
        }
    }

    private static bool TryInstallLegacy()
    {
        var toolbar = GetToolbarWindow();
        if (toolbar == null) return false;

        var root = toolbar.rootVisualElement;
        if (root == null) return false;

        var existing = root.Q<Button>(ButtonName);
        if (existing != null)
        {
            _legacyButton = existing;
            _legacyButton.clicked -= Toggle; // Ensure single listener
            _legacyButton.clicked += Toggle;
            ApplyLegacyVisual();
            return true;
        }

        var zone = FindFirstZone(root);
        if (zone == null) return false;

        _legacyButton = new Button(Toggle)
        {
            name = ButtonName,
            text = Label
        };

        _legacyButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        _legacyButton.style.marginLeft = 4;
        _legacyButton.style.marginRight = 4;
        _legacyButton.style.height = 18;

        ApplyLegacyVisual();
        zone.Insert(0, _legacyButton);
        Debug.Log("[MCP Toolbar] Successfully created and added legacy toolbar button.");
        return true;
    }

    private static VisualElement FindFirstZone(VisualElement root)
    {
        var names = new[]
        {
            "ToolbarZonePlayModes",
            "ToolbarZonePlayMode",
            "ToolbarZoneMiddleAlign",
            "ToolbarZoneCenterAlign",
            "ToolbarZoneLeftAlign"
        };

        for (int i = 0; i < names.Length; i++)
        {
            var z = root.Q<VisualElement>(names[i]);
            if (z != null)
            {
                Debug.Log($"[MCP Toolbar] Found zone via name: {names[i]}");
                return z;
            }
        }

        Debug.LogWarning("[MCP Toolbar] No suitable zone found for toolbar injection.");
        return null;
    }

    private static void ApplyLegacyVisual()
    {
        if (_legacyButton == null) return;

        if (_isRunning)
        {
            _legacyButton.style.backgroundColor = new StyleColor(new Color(0.8f, 0.2f, 0.2f, 1f));
            _legacyButton.style.color = new StyleColor(Color.white);
        }
        else
        {
            _legacyButton.style.backgroundColor = new StyleColor(new Color(0.2f, 0.8f, 0.2f, 1f));
            _legacyButton.style.color = new StyleColor(Color.black);
        }
    }

    private static EditorWindow GetToolbarWindow()
    {
        var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        if (toolbarType == null) return null;

        var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        if (toolbars == null || toolbars.Length == 0) return null;

        return toolbars[0] as EditorWindow;
    }

#endif
}