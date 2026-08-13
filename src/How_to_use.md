# Documentation & Configuration Guide

## Highlights & Features
- **New Theming System**
- **New Native Navigation Pane Option**
- **New Wallpaper Parallax Processing Method**
- **New Dynamic Sidebar Size**

---

## Recommended Setup

It is recommended to use the Windhawk mod **"Windows 11 File Explorer Styler"** to hide the default toolbar.

<details>
<summary><b>Windhawk Mod Configuration (XAML / Style Settings)</b></summary>

```yaml
theme: ''
backgroundTranslucentEffect: none
backgroundTranslucentEffectRegion: ''
styleConstants:
  - ''
controlStyles:
  - target: FileExplorerExtensions.FileExplorerTabControl
    styles:
      - Margin=0,100,0,0
  - target: TabViewItem > Grid#LayoutRoot
    styles:
      - CornerRadius=10
      - Margin=0,-5,0,0
      - Height=24
      - BorderBrush=#30000000
      - BorderThickness=1,1,1,1
      - MaxWidth=230
  - target: Button#AddButton
    styles:
      - Background=Transparent
  - target: Grid#TabContainerGrid > Border > Button#AddButton
    styles:
      - HorizontalAlignment=Right
      - Margin=-10,120,5,5
      - Height=24
      - Width=24
      - CornerRadius=12
      - BorderBrush=#30000000
      - BorderThickness=0
themeResourceVariables:
  - ''
explorerFrameContainerHeight: 50
xamlDiagnosticsHandling: ''
```

</details>

---

## Context Menu Controls

Right-clicking on the toolbar opens the menu with the following options:

| Option | Description |
| :--- | :--- |
| **Settings** | Opens `app_settings.json` using your default text editor. |
| **Apply settings** | Applies updated settings (excluding XAML theme changes). |
| **Relaunch app** | Relaunches the app to apply XAML changes or wallpaper updates. |
| **Exit App** | Quits the application. |

---

## Configuration Settings (`app_settings.json`)

Key properties explained:

- **`SidebarVisible`**
  - `0`: Hides the sidebar overlay (only dot and toolbar remain visible).
  - `1`: Shows the sidebar overlay.
- **`NativeNavPane`**
  - `0`: Use custom shortcuts. You can edit the JSON directly or drag folders to the sidebar near favorites.
  - `1`: Mirrors the native Explorer navigation pane. It is click-through, allowing normal navigation pane behavior.
- **`OuterCosmeticBorderVisible`**
  - `0`: Hides the outer cosmetic border.
  - `1`: Displays an extra cosmetic border. Corner radius can be adjusted within the XAML file.

---

## Creating Custom Themes & Toolbar Buttons

You can create your own theme or add customized toolbar buttons with hotkey functions.

### Example Custom Button (XAML):

```xml
<!-- Tag Icon Button Example -->
<Button
    x:Name="BtnTag"
    Width="28"
    Height="26"
    Style="{StaticResource NavButtonStyle}"
    Tag="%{ENTER}"
    ToolTip="Edit Tags">
    <Path
        Data="M 4,2 H 9 L 16,9 A 1,1 0 0 1 16,10.5 L 10.5,16 A 1,1 0 0 1 9,16 L 2,9 V 4 A 2,2 0 0 1 4,2 Z M 5.5,5.5 A 0.5,0.5 0 1 0 5.5,5.6 Z"
        Stroke="{Binding Foreground, RelativeSource={RelativeSource AncestorType=Button}}"
        StrokeLineJoin="Round"
        StrokeThickness="1.5" />
</Button>
```

> **Note on Customization:**
> - `x:Name`: Set a unique identifier name for the button.
> - `Tag`: Assign desired hotkey combination (e.g., `%{ENTER}`).
> - `ToolTip`: Tooltip text displayed on mouse hover.
> - `Data`: SVG Path vector data for the icon.

---

## Theme Presets & Recommended Configurations

### 1. `os27.xaml` (OS27 Beta Inspired)

#### Dark Mode Configuration:
```json
{
  "ThemeXamlPath": "Themes/os27.xaml",
  "SidebarVisible": 1,
  "NativeNavPane": 1,
  "OuterCosmeticBorderVisible": 0,
  "ToolbarPosX": -7,
  "ToolbarPosY": 10,
  "SidebarWidth": 190,
  "CapsuleBackgroundBrush": "#191919",
  "MainTextBrush": "#ffffff",
  "OuterFrameBrush": "#191919",
  "Layer3BorderBackground": {
    "ColorHex": "#191919",
    "Opacity": 0.85
  },
  "LeftSidebarGridShadow": {
    "BlurRadius": 20,
    "Opacity": 0,
    "ShadowDepth": 5,
    "ColorHex": "#000000"
  }
}
```

#### Light Mode Configuration:
```json
{
  "ThemeXamlPath": "Themes/os27.xaml",
  "SidebarVisible": 1,
  "NativeNavPane": 1,
  "OuterCosmeticBorderVisible": 0,
  "ToolbarPosX": -7,
  "ToolbarPosY": 10,
  "SidebarWidth": 190,
  "CapsuleBackgroundBrush": "White",
  "MainTextBrush": "#191919",
  "OuterFrameBrush": "White",
  "Layer3BorderBackground": {
    "ColorHex": "White",
    "Opacity": 0.85
  },
  "LeftSidebarGridShadow": {
    "BlurRadius": 20,
    "Opacity": 0,
    "ShadowDepth": 5,
    "ColorHex": "#000000"
  }
}
```

---

### 2. `default.xaml` (OS26 Inspired)

#### Dark Mode Configuration:
```json
{
  "ThemeXamlPath": "Themes/default.xaml",
  "SidebarVisible": 1,
  "NativeNavPane": 1,
  "OuterCosmeticBorderVisible": 1,
  "ToolbarPosX": -3,
  "ToolbarPosY": 7,
  "SidebarWidth": 190,
  "CapsuleBackgroundBrush": "#191919",
  "MainTextBrush": "#ffffff",
  "OuterFrameBrush": "#191919",
  "Layer3BorderBackground": {
    "ColorHex": "#191919",
    "Opacity": 0.85
  },
  "LeftSidebarGridShadow": {
    "BlurRadius": 20,
    "Opacity": 0.15,
    "ShadowDepth": 5,
    "ColorHex": "#000000"
  }
}
```
