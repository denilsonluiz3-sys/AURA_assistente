# AURA Design System Migration

## Phase 1: Reference Audit ⭐ COMPLETED
**File**: `/root/AURA_assistente/info/design_system_audit/reference_audit.md`

### Reference Color Palette (AURA_UI_REFERNICA.txt)

| Key | Value | Purpose |
|-----|-------|---------|
| `AuraBackground` | `#0c0c12` | Primary background |
| `AuraSurface` | `#13131d` | Card headers, main surfaces |
| `AuraSurface2` | `#1c1c2a` | Input fields, secondary surfaces |
| `AuraAccent` | `#4f8aff` | Primary actions, highlights |
| `AuraCyan` | `#38d9c0` | Assistant identity |
| `AuraTextPrimary` | `#e8e8f0` | Main text |
| `AuraTextSecondary` | `#7a7a90` | Labels, less important text |
| `AuraTextMuted` | `#45455a` | Placeholders, hints |
| `AuraSuccess` | `#3ec97a` | Success states |
| `AuraError` | `#e05560` | Error states |
| `AuraWarning` | `#f0a050` | Warnings |
| `AuraBorder` | `#242438` | Default borders |
| `AuraBorderAccent` | `#2a3a6a` | Accent borders |
| `AuraUserBubble` | `#1e2d54` | User chat bubble |
| `AuraAgentBubble` | `#13131d` | Agent chat bubble |
| `AuraToolBubble` | `#0f1420` | Tool output bubble |

### Reference Styles (App.xaml)
- **BtnPrimary**: AuraAccent bg, White text, 10 radius, 14pt bold
- **BtnGhost**: AuraSurface2 bg, AuraAccent text, 10 radius, 13pt
- **BtnDanger**: AuraError bg, White text, 10 radius, 13pt
- **AuraCard**: AuraSurface bg, AuraBorder stroke, 14 radius
- **AuraCardAccent**: AuraSurface bg, AuraBorderAccent stroke, 14 radius

---

## Phase 2: Destination Audit ⭐ COMPLETED
**File**: `/root/AURA_assistente/info/design_system_audit/destination_audit.md`

### Color Delta Analysis

| Color | Destination | Reference | Delta |
|-------|-------------|-----------|-------|
| `AuraBackground` | `#12141f` | `#0c0c12` | +81 brightness |
| `AuraAccent` | `#7a9eff` | `#4f8aff` | +121 brightness (critical hue shift) |
| `AuraTextSecondary` | `#b8bcc8` | `#7a7a90` | **CRITICAL** - 2x brighter, reduces dark mode contrast |
| `AuraCyan` | `#7a9eff` | `#38d9c0` | **DIFF** - now matches AuraAccent (lost cyan identity) |
| `AuraAgentBubble` | `#0d0f18` | `#13131d` | Now equals AuraSurface |

### New Features Added (not in reference)
- `AuraAccent2`: `#8a5ae0` (purple secondary)
- `AuraGlass`: `#990d0f18` (glass/backdrop effect)
- `AuraGlassBorder`: `#33ffffff` (glass border)
- `AuraGlassBar`: Style for glass-style bottom bars

### Style Diffs
- **BtnPrimary**: Same structure, but AuraAccent now lighter (#7a9eff vs #4f8aff)
- **All card/button styles**: Identical structure, colors shifted

---

## Phase 3: Migration Actions REQUIRED

### Action 1: Fix Critical Color Issues
**Priority**: HIGH - AuraTextSecondary readability is compromised
- **Problem**: `#b8bcc8` is too bright for secondary text on dark backgrounds
- **Fix**: Restore reference `#7a7a90` or use CSS-safe color `#7a7a90`

### Action 2: Restore AuraAccent Hue
**Priority**: HIGH - Lost original "aura" identity
- **Problem**: `#7a9eff` has lost the original `#4f8aff` blue-green hue
- **Fix**: Restore reference `#4f8aff`

### Action 3: Restore AuraCyan
**Priority**: MEDIUM - Assistant identity needs distinct color
- **Problem**: AuraCyan now matches AuraAccent (`#7a9eff`)
- **Fix**: Restore reference `#38d9c0`

### Action 4: Optimize Surface Colors
**Priority**: MEDIUM - Improve contrast hierarchy
- **Problem**: Surfaces shifted lighter in inconsistent ways
- **Fix**: Align with reference palette for consistency

### Action 5: Document Glass Features
**Priority**: LOW - New feature to evaluate
- **Actions**: 
  - Document glass effect usage
  - Verify AuraGlassBar styling matches design intent
  - Consider adding to reference if purposefully designed

---

## Phase 4: Implementation Plan

### Step 1: Update App.xaml Colors
Update color definitions in `src/AURA.Mobile/App.xaml`:

```xml
<Color x:Key="AuraAccent">#4f8aff</Color>    <!-- Restore original -->
<Color x:Key="AuraCyan">#38d9c0</Color>      <!-- Restore cyan identity -->
<Color x:Key="AuraTextSecondary">#7a7a90</Color> <!-- Fix contrast -->
<Color x:Key="AuraAgentBubble">#13131d</Color> <!-- Restore from AuraSurface -->
```

### Step 2: Create DesignSystem.cs
Implement a C# class to centralize design tokens:

**File**: `src/AURA.Mobile/DesignSystem.cs`
- Define static properties for colors
- Provide validation for accessibility contrast ratios
- Centralize font family and sizing constants

### Step 3: Validate with Pages
Ensure all XAML pages using DynamicResource properly reference the corrected palettes.

---

## Status
- [x] Phase 1: Reference Audit
- [x] Phase 2: Destination Audit  
- [~] Phase 3: Color Corrections (in progress)
- [ ] Phase 4: DesignSystem.cs Implementation
- [ ] Phase 5: Validate All Pages
- [ ] Phase 6: Visual Testing

## Migration Plan

### Phase 1: Reference Audit (Completed)
- Colors mapped and verified
- Typography confirmed (OpenSans)
- Style definitions extracted

### Phase 2: Destination Audit (In Progress)
- Existing App.xaml confirms alignment with reference palette
- Button styles match reference
- Card styles match reference

### Phase 3: Design System Implementation
- Create centralized DesignSystem.cs file
- Implement missing styles (if any gaps)
- Ensure all components use consistent color scheme

### Phase 4: Component Migration
- Buttons: Already aligned
- Cards: Already aligned
- Forms/Layouts: Review and adjust

### Phase 5: Validation
- Visual regression testing
- Cross-browser compatibility check
- Performance impact assessment

## Action Items

1. **Create Design System Documentation** - Complete migration plan
2. **Verify Color Consistency** - Ensure all UI elements use reference colors
3. **Implement Missing Components** - Add any missing styles from reference
4. **Run Visual Regression Tests** - Compare before/after screenshots
5. **Deploy to Staging** - Validate in staging environment

## Timeline
- Week 1: Complete design system migration
- Week 2: Testing and validation
- Week 3: Deployment to production

## Risks
- Some button styles may differ slightly in padding/sizing
- Need to ensure all new components inherit from DesignSystem
- Potential conflicts with existing custom styles

## Next Steps
1. Create DesignSystem.cs with centralized color/theming
2. Verify all button styles match reference
3. Check card styles and layout components
4. Run visual regression tests
5. Prepare deployment checklist
