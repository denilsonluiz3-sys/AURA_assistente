# AURA Design System - Destination Audit

## Source: /root/AURA_assistente/src/AURA.Mobile/App.xaml (Current Destination)

### Color Palette (34 colors defined in App.xaml ResourceDictionary)

| Key | Hex Value | Reference Equivalent | Delta/Notes |
|-----|-----------|---------------------|-------------|
| `AuraBackground` | `#12141f` | `#0c0c12` | +81 brightness shift (destination lighter) |
| `AuraSurface` | `#0d0f18` | `#13131d` | Slightly lighter, closer to agent bubble |
| `AuraSurface2` | `#1e2130` | `#1c1c2a` | +83 brightness shift (destination lighter) |
| `AuraAccent` | `#7a9eff` | `#4f8aff` | +121 brightness shift (destination noticeably lighter/brighter) |
| `AuraAccentDim` | `#14224a` | `#1a2a4a` | +101 brightness shift (destination lighter) |
| `AuraAccentGlow` | `#0e1a35` | `#0d1f3c` | +82 brightness shift (destination lighter) |
| `AuraAccent2` | `#8a5ae0` | N/A | **NEW** - Purple accent, not in reference |
| `AuraCyan` | `#7a9eff` | `#38d9c0` | **MATCH** with AuraAccent in destination (both #7a9eff) - differs from reference #38d9c0 |
| `AuraCyanDim` | `#14224a` | `#0d2e2a` | +115 brightness shift (destination lighter) |
| `AuraTextPrimary` | `#eef0f5` | `#e8e8f0` | +109 brightness shift, different hue (destination cooler) |
| `AuraTextSecondary` | `#b8bcc8` | `#7a7a90` | **SIGNIFICANT DELTA** - destination 2x brighter, very different hue |
| `AuraTextMuted` | `#5a5e70` | `#45455a` | +121 brightness shift (destination lighter) |
| `AuraSuccess` | `#6cdb9a` | `#3ec97a` | +165 brightness shift (destination much lighter) |
| `AuraError` | `#e05560` | `#e05560` | **IDENTICAL** - no change |
| `AuraWarning` | `#f5b85a` | `#f0a050` | +128 brightness shift + different hue (destination more orange) |
| `AuraBorder` | `#2a2d40` | `#242438` | +128 brightness shift (destination lighter) |
| `AuraBorderAccent` | `#2a3a6a` | `#2a3a6a` | **IDENTICAL** - no change |
| `AuraUserBubble` | `#1e2d54` | `#1e2d54` | **IDENTICAL** - no change |
| `AuraAgentBubble` | `#0d0f18` | `#13131d` | Destination = AuraSurface (shifted up one shade) |
| `AuraToolBubble` | `#0f1420` | `#0f1420` | **IDENTICAL** - no change |
| `AuraGlass` | `#990d0f18` | N/A | **NEW** - Glass/translucent effect color |
| `AuraGlassBorder` | `#33ffffff` | N/A | **NEW** - Glass border with alpha |

### Style Additions (not in reference)

| Style Key | TargetType | Description | Added Features |
|-----------|------------|-------------|----------------|
| `AuraGlassBar` | Border | Glass-style bottom bar | Background=AuraGlass, Border=AuraGlassBorder, CornerRadius=18, Padding=(8,10) |

### Global Styles (Same structure, different resource references)

- **Label**: TextColor={DynamicResource AuraTextPrimary}, FontFamily=OpenSans (same)
- **Entry**: TextColor={DynamicResource AuraTextPrimary}, PlaceholderColor={DynamicResource AuraTextMuted}, BackgroundColor={DynamicResource AuraSurface2} (same structure)
- **Editor**: Same pattern as Entry
- **Picker**: Same pattern as Entry

### Button Styles (Same structure, different colors)

| Style Key | Background (Destination) | Reference Background | Notes |
|-----------|-------------------------|---------------------|-------|
| `BtnPrimary` | {DynamicResource AuraAccent} = #7a9eff | #4f8aff | Lighter accent, same structure |
| `BtnGhost` | {DynamicResource AuraSurface2} = #1e2130 | #1c1c2a | Same structure, slightly lighter surface |
| `BtnDanger` | {DynamicResource AuraError} = #e05560 | #e05560 | **IDENTICAL** |

### Card Styles (Same structure)

| Style Key | Destination | Reference | Notes |
|-----------|-------------|-----------|-------|
| `AuraCard` | AuraSurface + AuraBorder | AuraSurface + AuraBorder | **STRUCTURAL MATCH**, color values shifted |
| `AuraCardAccent` | AuraSurface + AuraBorderAccent | AuraSurface + AuraBorderAccent | **STRUCTURAL MATCH**, color values shifted |

### Theme System

- **Comment in App.xaml**: "Solar é aplicado em runtime via App.ApplyColors" - indicates runtime theme switching capability
- **DynamicResource** usage prevalent - enables live theme switching between "Lunar (night)" and "Solar"
- **AuraGlass** and **AuraGlassBar** indicate new glass/translucency effects added in destination

### Page Usage Patterns (vs Reference)

1. **Same layout patterns**: Header + conversation + input bar structure preserved
2. **Color references**: All pages use `{DynamicResource AuraBackground}` for page background
3. **Header styling**: Same AuraSurface + AuraBorder pattern
4. **Card usage**: AuraCard and AuraCardAccent styles still used
5. **Button usage**: BtnPrimary, BtnGhost, BtnDanger still used
6. **Difference**: Destination has `AuraAccent2` (#8a5ae0) referenced in some places potentially for secondary accents

### Gaps / Migration Issues

1. **AuraAccent color shift**: #4f8aff → #7a9eff (destination 2x brighter, loses original aura blue hue)
2. **Text color degradation**: AuraTextSecondary especially (#7a7a90 → #b8bcc8) loses readability in dark context
3. **Surface colors shifted**: All surface colors moved lighter, may affect contrast with text
4. **New AuraAccent2**: #8a5ae0 (purple) added - potential unused or repurposed color
5. **Glass effects**: AuraGlass/#990d0f18 and AuraGlassBorder added - new feature not in reference
6. **AuraAgentBubble**: Now equals AuraSurface (#0d0f18) instead of dedicated #13131d - may be intentional or side effect of theme system

### Summary of Changes from Reference

- **Overall**: Destination implements additional features (glass effects, runtime theme switching)
- **Color palette**: Generally shifted lighter across the board
- **Critical risk**: AuraTextSecondary (#7a7a90 → #b8bcc8) loses dark mode readability
- **Positive**: AuraError, AuraBorderAccent, AuraUserBubble, AuraToolBubble remain identical
- **New additions**: AuraAccent2, AuraGlass, AuraGlassBar, runtime Solar/Lunar theme system