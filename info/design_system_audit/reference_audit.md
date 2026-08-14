# AURA Design System - Reference Audit

## Source: AURA_UI_REFERNCIA.txt (src/AURA.Mobile/ from reference repo)

### Color Palette (33 colors defined in App.xaml ResourceDictionary)

| Key | Hex Value | Usage |
|-----|-----------|---------|
| `AuraBackground` | `#0c0c12` | Primary app background |
| `AuraSurface` | `#13131d` | Main surface color (cards, headers) |
| `AuraSurface2` | `#1c1c2a` | Secondary surface (inputs, borders) |
| `AuraAccent` | `#4f8aff` | Primary accent color (primary buttons, highlights) |
| `AuraAccentDim` | `#1a2a4a` | Dimmed accent variant |
| `AuraAccentGlow` | `#0d1f3c` | Accent glow effect |
| `AuraCyan` | `#38d9c0` | Cyan accent (assistant identity) |
| `AuraCyanDim` | `#0d2e2a` | Dimmed cyan variant |
| `AuraTextPrimary` | `#e8e8f0` | Primary text color |
| `AuraTextSecondary` | `#7a7a90` | Secondary text color |
| `AuraTextMuted` | `#45455a` | Muted/placeholder text |
| `AuraSuccess` | `#3ec97a` | Success state color |
| `AuraError` | `#e05560` | Error state color |
| `AuraWarning` | `#f0a050` | Warning state color |
| `AuraBorder` | `#242438` | Default border color |
| `AuraBorderAccent` | `#2a3a6a` | Accent-colored border |
| `AuraUserBubble` | `#1e2d54` | User message bubble background |
| `AuraAgentBubble` | `#13131d` | Agent/assistant message bubble background |
| `AuraToolBubble` | `#0f1420` | Tool/system message bubble background |

### Global Styles (TargetType styles)

- **Label**: TextColor=AuraTextPrimary, FontFamily=OpenSans
- **Entry**: TextColor=AuraTextPrimary, PlaceholderColor=AuraTextMuted, BackgroundColor=AuraSurface2
- **Editor**: TextColor=AuraTextPrimary, PlaceholderColor=AuraTextMuted, BackgroundColor=AuraSurface2
- **Picker**: TextColor=AuraTextPrimary, BackgroundColor=AuraSurface2, TitleColor=AuraTextMuted

### Button Styles

| Style Key | Description | Background | Text | CornerRadius | Padding | FontSize | FontAttributes |
|-----------|-------------|------------|------|--------------|---------|----------|----------------|
| `BtnPrimary` | Primary accent button | AuraAccent | White | 10 | (16,10) | 14 | Bold |
| `BtnGhost` | Ghost/button outline | AuraSurface2 | AuraAccent | 10 | (14,8) | 13 | — |
| `BtnDanger` | Destructive action | AuraError | White | 10 | (14,8) | 13 | — |

### Card Styles

| Style Key | TargetType | Background | Stroke | StrokeThickness | Padding | CornerRadius |
|-----------|------------|------------|--------|-----------------|---------|--------------|
| `AuraCard` | Border | AuraSurface | AuraBorder | 1 | (16,14) | 14 |
| `AuraCardAccent` | Border | AuraSurface | AuraBorderAccent | 1 | (16,14) | 14 |

### Page Layout Patterns (observed across all XAML files)

1. **Header pattern**: Border with AuraSurface background, AuraBorder stroke, RoundRectangle 0,0,0,0 stroke shape
2. **Conversation area**: ScrollView with VerticalStackLayout, spacing=6, Padding=(14,12)
3. **Input bar**: Bottom Border with AuraSurface, AuraBorder stroke, RoundRectangle with corner radii
4. **Bubble rendering**: Code-behind (AgentPage.xaml.cs) computes colors from reference palette:
   - User bubble: `#1e2d54` (AuraUserBubble)
   - Agent bubble: `#13131d` (AuraAgentBubble)
   - Tool bubble: `#0f1420` (AuraToolBubble)
   - Stroke colors: AuraBorderAccent for user, AuraBorder for agent/tool
   - Text: AuraTextPrimary for normal, AuraTextSecondary for tool

### Resources Dynamic vs Static

- `DynamicResource` used for data-binding scenarios (pages may swap themes at runtime)
- `StaticResource` used for fixed style definitions (buttons, cards)

### Additional Observations

1. **Color consistency**: All 27+ colors follow a dark mode palette with #0c0c12 as the deepest background
2. **Accent hierarchy**: AuraAccent (#4f8aff) is primary action color; AuraCyan (#38d9c0) is assistant identity
3. **Border system**: Two-tier border system - AuraBorder (default) + AuraBorderAccent (accent highlight)
4. **Bubble system**: Distinct background colors for user/agent/tool messages with coordinating strokes
5. **No glass/transparency effects** in reference - all opaque colors