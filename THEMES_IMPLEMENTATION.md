# Theme Support in Loot Crates - Implementation Summary

## Overview
Successfully implemented a non-destructive, backward-compatible system to add themes as loot crate rewards alongside collectibles in your video library system.

## Key Features Implemented

### 1. **Models Created/Extended**

#### New Classes:
- **`ThemeRewardDefinition`** (Models/CollectibleDefinition.cs)
  - Defines theme rewards available in loot crates
  - Properties: `Id`, `Name`, `Description`, `IconGlyph`, `DropWeight`, `Rarity`, `IsDark`, `ResourceUri`
  - Extended properties for customization:
	- `ImageUrl`: Optional preview/background image
	- `RelatedImageUrls`: List of additional images (e.g., wood panel textures)
	- `ThemeData`: Dictionary for theme metadata (colors, settings, configuration)

- **`CollectedTheme`** (Models/BackRoomsState.cs)
  - Represents a theme collected/unlocked from a loot crate
  - Tracks: `Id`, `Name`, `Description`, `CollectedDateUtc`, `Rarity`, `ThemeData`

- **`LootCrateReward`** (Models/LootCrateReward.cs)
  - Union-type representing either a collectible or theme reward
  - Properties: `IsTheme` (boolean), `Collectible`, `Theme`
  - Factory methods: `FromCollectible()`, `FromTheme()`

#### Extended Classes:
- **`BackRoomsState`**
  - Added: `List<CollectedTheme> CollectedThemes` to track user-collected themes

- **`CollectibleDefinition`**
  - Remains unchanged for backward compatibility

### 2. **JSON Format - Non-Destructive Update**

#### New Structure (with backward compatibility):
```json
{
  "collectibles": [...],
  "themes": [...]
}
```

#### Backward Compatibility:
- Old format (array of collectibles only) still supported
- `CollectiblesStore.ParseCollectiblesJson()` intelligently detects format:
  - If root is object: parse as new format with both arrays
  - If root is array: treat as legacy collectibles-only format
  - Returns tuple: `(List<CollectibleDefinition>, List<ThemeRewardDefinition>)`

#### Sample Themes in JSON:
```json
{
  "id": "Retro",
  "name": "Retro 90s",
  "description": "Neon-soaked nostalgia from the 1990s...",
  "iconGlyph": "◉",
  "dropWeight": 15,
  "rarity": "Uncommon",
  "isDark": true,
  "resourceUri": "/ResourceDictionaries/RetroTheme.xaml",
  "imageUrl": null,
  "relatedImageUrls": null,
  "themeData": {
	"accentColor": "#FF00FF",
	"secondaryColor": "#00FFFF",
	"style": "cyberpunk"
  }
}
```

### 3. **Service Enhancements**

#### `BackRoomsService` Updates:

**New Methods:**
- **`LoadCollectiblesAndThemesAsync()`**
  - Loads both collectibles and themes from configured source
  - Returns: `(List<CollectibleDefinition>, List<ThemeRewardDefinition>)`
  - Supports same URL schemes: http/https, file://, local paths

- **`OpenLootCrateWithThemesAsync()`**
  - Unified loot crate opening with combined collectible + theme pool
  - Parameters: `BackRoomsState`, `availableCollectibles`, `availableThemes`
  - Returns: `LootCrateReward?` (either collectible or theme)
  - Features:
	- Deducts 1 gem
	- Weighted random selection from combined pools
	- Downloads/caches theme and collectible images
	- Automatically adds rewards to state
	- Refunds gem on failure

- **`SelectRandomDefinition()`** (private)
  - Weighted random selection from combined collectible + theme pools
  - Uses drop weights for probability
  - Returns: `RewardDefinition` helper object

#### `CollectiblesStore` Updates:

**New Methods:**
- **`AddTheme()`** - Add and persist collected theme
- **`ParseCollectiblesJson()`** - Static parser supporting both old and new JSON formats

**Internal Class:**
- **`CollectiblesJsonWrapper`** - For deserializing new object format

### 4. **Backward Compatibility**

The implementation ensures **zero breaking changes**:

✅ **Existing JSON files remain unchanged** - Just add a "themes" array
✅ **Old loading code still works** - Falls back to array-only parsing
✅ **Fallback collectibles unchanged** - Works with new format
✅ **Existing collectibles collections** - Continue to function normally
✅ **Default themes (Light, Dark)** - Remain hardcoded in ThemeManager

## Usage Examples

### Loading Collectibles and Themes:
```csharp
var service = new BackRoomsService(store);
var (collectibles, themes) = await service.LoadCollectiblesAndThemesAsync(sourceUrl);
```

### Opening Loot Crate (New Unified Method):
```csharp
var reward = await service.OpenLootCrateWithThemesAsync(state, collectibles, themes);

if (reward?.IsTheme == true)
{
	// User won a theme
	var theme = reward.Theme;
	Console.WriteLine($"Won theme: {theme.Name}");
}
else
{
	// User won a collectible
	var collectible = reward?.Collectible;
	Console.WriteLine($"Won collectible: {collectible?.Name}");
}
```

### Accessing Collected Themes:
```csharp
var collectedThemes = backRoomsState.CollectedThemes;
foreach (var theme in collectedThemes)
{
	Console.WriteLine($"{theme.Name} ({theme.Rarity}) - {theme.Description}");
}
```

## Configuration Options

### Theme Metadata via ThemeData:
```json
"themeData": {
  "accentColor": "#FF00FF",
  "woodTone": "dark-walnut",
  "grainPattern": "natural",
  "style": "cyberpunk"
}
```

### Theme Images:
- **Single preview image**: `imageUrl` property
- **Multiple related images**: `relatedImageUrls` array (textures, patterns, backgrounds)
- All support http/https URLs (downloaded same as collectibles)

## Drop Weight System

Both collectibles and themes use the same `dropWeight` system:
- Higher weight = more common
- Combined weighted random selection when opening crates
- Default weight: 1
- Example: If collectibles total 100 weight and themes total 50, themes have ~33% chance

## Files Modified

1. **Models/CollectibleDefinition.cs** - Added `ThemeRewardDefinition` class
2. **Models/BackRoomsState.cs** - Added `CollectedTheme` class and collection
3. **Models/LootCrateReward.cs** - Created new file
4. **Services/CollectiblesStore.cs** - Added theme support and JSON parsing
5. **Services/BackRoomsService.cs** - Added theme loading and combined reward system
6. **SampleData/collectibles.json** - Updated to new format with themes
7. **SampleData/fallback-collectibles.json** - Updated to new format

## Build Status
✅ Successfully compiles with no errors or warnings

## Next Steps (Optional)

1. **UI Integration** - Update MainWindow to display theme rewards
2. **Theme Application** - Implement UI for applying collected themes
3. **Theme Customization** - Allow users to apply collected themes
4. **Additional Themes** - Add more theme definitions to JSON
5. **Performance** - Consider caching theme metadata if needed

## Notes

- Light and Dark themes remain hardcoded defaults and are not included in collected themes
- Theme rewards are non-duplicative by default (user can have multiple copies in state)
- All theme images/assets use same download/cache system as collectibles
- Theme metadata stored in `ThemeData` dictionary for extensibility
