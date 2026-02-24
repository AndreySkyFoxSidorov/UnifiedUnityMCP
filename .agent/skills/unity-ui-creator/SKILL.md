---
name: unity-ui-creator
description: UI design and implementation in Unity (uGUI/Canvas). Use when you need to build/layout a screen/menu/HUD/popup/window, create Button/Dropdown/InputField/Canvas/Scroll View/Scrollbar/Slider/Toggle/Panel/RawImage/Text/Image.
compatibility: Requires access to the UI scene/prefabs, a list of target resolutions, and orientation (Portrait, Landscape, or Universal).
metadata:
  language: en
  domain: unity
  ui: ugui
  version: "1.0.0"
---

# Unity UI Creator

## Goal
Build a UI screen in Unity **fast and clean**: proper Canvas structure, responsive layout, readable prefabs, minimal overhead, correct setup for Button/Dropdown/InputField/Canvas/ScrollView/Scrollbar/Slider/Toggle/Panel/Raw Image/Text/Image.

## When to use
- You need to create a new screen (Menu/Shop/Settings/HUD/Popup)
- You need to build/layout (screen/menu/HUD/popup/window)
- You need to add (Button/Dropdown/InputField/Canvas/ScrollView/Scrollbar/Slider/Toggle/Panel/Raw Image/Text/Image)

## What to ask from the user (minimum)
1) Screen goal + list of elements (Button/Dropdown/InputField/Canvas/ScrollView/Scrollbar/Slider/Toggle/Panel/RawImage/Text/Image)
2) Target platforms: orientation (portrait/landscape)
3) List of key resolutions (e.g., 1080x1920, 1170x2532, 1440x3200)

## Recommended prefab hierarchy structure
- **UI{Screen_Name}** (Components: "RectTransform" + "Canvas" + "GraphicRaycaster" + "UI Base Animations Behaviour (Script)" + "UI{Screen_Name} View (Script)")
  - **Background** (Components: "RectTransform" + Image)
    - **LeftAnchor** (root container for all elements that must be anchored to the left side of the screen)
    - **RightAnchor** (root container for all elements that must be anchored to the right side of the screen)
    - **LowerAnchor** (root container for all elements that must be anchored to the bottom of the screen)
    - **UpperAnchor** (root container for all elements that must be anchored to the top of the screen)
    - **PopupAnchor** (root container for elements if the window is a popup)

## UI Organization
```
Prefabs/UI/
├── Common/
│   ├── UI_Button.prefab
│   ├── UI_Panel.prefab
│   └── UI_Text.prefab
├── Screens/
│   ├── UI_MainMenu.prefab
│   ├── UI_Settings.prefab
│   └── UI_GameOver.prefab
└── HUD/
    ├── UI_HealthBar.prefab
    └── UI_Minimap.prefab
```

## Checklist before delivery
- [ ] Tested on at least 3 aspect ratios (16:9, 19.5:9, 4:3)
- [ ] Texts are not clipped (localization: long strings)
- [ ] No Layout “jitter” on open (visually and in the Profiler)
- [ ] Screen is a prefab, with no hard scene references

## Response format (how I should respond)
Always provide:
1) Proposed hierarchy structure (short)
2) Canvas Scaler settings + Match rationale
3) Layout decisions (where to use LayoutGroup, where to do it manually)
4) Performance risks and what to simplify
5) Minimal implementation plan (steps)

## Example task and solution

### Input
“Need a shop screen: balance at the top, below it a scrollable list of items, and a ‘Buy’ button at the bottom. On iPhone with a notch the UI overlaps.”

### Output
1) `Canvas Scaler = Scale With Screen Size`, reference 1080x1920, Match 0.5  
2) ScrollRect: Viewport+Mask, Content with VerticalLayoutGroup + ContentSizeFitter (only on Content)

## Troubleshooting
- If elements “shift”: check anchors/pivots, and don’t mix fixed positions with LayoutGroup.
- If the scroll jerks: remove extra ContentSizeFitter components, check Layout component order.
- If everything is slow: find the rebuild source, split the Canvas, reduce Layout, make updates event-driven.



## Routing Index
- Source mirror: `scripts/`


## Scope
This skill is a guide and does not provide direct MCP automation commands by itself.
