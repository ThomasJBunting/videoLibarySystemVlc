# Back Rooms Navigation - Detailed Flow Documentation

## Visual Navigation Flow

### Old Behavior (BEFORE FIX) ❌
```
User clicks Collectible
		↓
SelectedCollectible property updated
		↓
Detail panel shows collectible
		↓
❌ BUT: Section tab stays the same!
   If on "Loot Crate Info" tab, it stays there
   If on "Themes" tab, it stays there
   User gets confused
```

### New Behavior (AFTER FIX) ✅
```
User clicks Collectible
		↓
CollectiblesGrid_SelectionChanged fires
		↓
SelectCollectibleAndSwitchTab() called
		↓
BackRoomsSectionList.SelectedIndex = 0 (Collectibles)
		↓
BackRoomsSectionList_SelectionChanged fires
		↓
CollectiblesSection.Visibility = Visible
ThemesSection.Visibility = Collapsed
LootCrateInfoSection.Visibility = Collapsed
		↓
SelectedCollectible = collectible
		↓
✅ Detail panel shows collectible AND tab is correct!
```

## Event Sequence Diagram

### Collectibles Click Path
```
┌─────────────────────────────────────────────────────────┐
│ User clicks Collectible Card in Grid                     │
└──────────────────┬──────────────────────────────────────┘
				   │
				   ↓
		 ┌─────────────────────┐
		 │ ListBox fires        │
		 │ SelectionChanged    │
		 └──────────┬──────────┘
					│
					↓
		┌──────────────────────────┐
		│ CollectiblesGrid_        │
		│ SelectionChanged()        │
		│ [Event Handler]           │
		└────────┬─────────────────┘
				 │
				 ↓
		┌──────────────────────────┐
		│ if (e.AddedItems[0]      │
		│     is Collectible)       │
		└────────┬─────────────────┘
				 │
				 ↓ YES
		┌──────────────────────────┐
		│ SelectCollectible         │
		│ AndSwitchTab()            │
		│ (collectible)             │
		└─┬──────────────────────┬──┘
		  │ Sets section         │ Sets selection
		  │ index to 0           │ index
		  ↓                      ↓
   BackRoomsSectionList    SelectedCollectible
		Selection          property updated
		  │                     │
		  ↓                     ↓
   BackRoomsSectionList_   Detail panel
   SelectionChanged()      shows item
		  │
		  ↓
   Switch visibility
   (show Collectibles,
	hide others)
		  │
		  ↓
	 ✅ RESULT:
   Collectibles tab active
   Detail panel shows item
```

### Themes Click Path
```
Same flow but:
- ThemesGrid_SelectionChanged() is called
- SelectThemeAndSwitchTab() sets index to 1 (Themes)
- ThemesSection visibility is set to Visible
- SelectedTheme property is updated
```

## Code Structure

### Event Handlers (Detect Clicks)
```
CollectiblesGrid_SelectionChanged(sender, e)
	↓
	Checks if e.AddedItems contains a Collectible
	↓
	Calls SelectCollectibleAndSwitchTab()

ThemesGrid_SelectionChanged(sender, e)
	↓
	Checks if e.AddedItems contains a ThemeDefinition
	↓
	Calls SelectThemeAndSwitchTab()
```

### Tab Switch Methods (Do the Work)
```
SelectCollectibleAndSwitchTab(collectible)
	├─ if (BackRoomsSectionList != null)
	│  └─ BackRoomsSectionList.SelectedIndex = 0
	└─ SelectedCollectible = collectible

SelectThemeAndSwitchTab(theme)
	├─ if (BackRoomsSectionList != null)
	│  └─ BackRoomsSectionList.SelectedIndex = 1
	└─ SelectedTheme = theme
```

### Visibility Controller (Backend of UI Sections)
```
BackRoomsSectionList_SelectionChanged(sender, e)
	↓
	Gets selectedIndex
	↓
	if (selectedIndex == 0)
	├─ CollectiblesSection.Visibility = Visible
	├─ ThemesSection.Visibility = Collapsed
	└─ LootCrateInfoSection.Visibility = Collapsed

	if (selectedIndex == 1)
	├─ CollectiblesSection.Visibility = Collapsed
	├─ ThemesSection.Visibility = Visible
	└─ LootCrateInfoSection.Visibility = Collapsed

	if (selectedIndex == 2)
	├─ CollectiblesSection.Visibility = Collapsed
	├─ ThemesSection.Visibility = Collapsed
	└─ LootCrateInfoSection.Visibility = Visible
```

## XAML Event Binding

### Collectibles ListBox
```xml
<ListBox Grid.Row="2"
		 ItemsSource="{Binding BackRoomsCollectibles}" 
		 SelectedItem="{Binding SelectedCollectible}"
		 SelectionChanged="CollectiblesGrid_SelectionChanged"  ← NEW
		 ...>
```

### Themes ListBox
```xml
<ListBox Grid.Row="1"
		 ItemsSource="{Binding AvailableThemesList}" 
		 SelectedItem="{Binding SelectedTheme}"
		 SelectionChanged="ThemesGrid_SelectionChanged"  ← NEW
		 ...>
```

## State Diagram

### Before Fix
```
					┌─────────────┐
					│   Anywhere  │
					└──────┬──────┘
						   │
		  Click Collectible │
						   ↓
			  ┌────────────────────────┐
			  │ Detail shows collectible│
			  │ But no tab switch ✗     │
			  │                         │
			  │ (User confused)         │
			  └────────────────────────┘
```

### After Fix
```
		   ┌──────────────┐        ┌──────────────┐        ┌──────────────┐
		   │ Collectibles │        │    Themes    │        │  Loot Crate  │
		   │    Active    │        │   Inactive   │        │   Inactive   │
		   └──────────────┘        └──────────────┘        └──────────────┘
					↑
					│ Click collectible
					│
			  ┌─────────────────────────┐
			  │ SelectCollectible       │
			  │ AndSwitchTab()          │
			  └─────────────────────────┘
					│
					↓
		   ┌──────────────┐        ┌──────────────┐        ┌──────────────┐
		   │ Collectibles │        │    Themes    │        │  Loot Crate  │
		   │    Active ✓  │        │   Inactive   │        │   Inactive   │
		   │  Detail: Yes │        │              │        │              │
		   └──────────────┘        └──────────────┘        └──────────────┘
```

## Testing Checklist

### Unit Level
- [x] CollectiblesGrid_SelectionChanged checks for Collectible type
- [x] ThemesGrid_SelectionChanged checks for ThemeDefinition type
- [x] SelectCollectibleAndSwitchTab sets correct index (0)
- [x] SelectThemeAndSwitchTab sets correct index (1)
- [x] Both methods handle null BackRoomsSectionList safely

### Integration Level  
- [ ] Click collectible → automatically on Collectibles tab
- [ ] Click theme → automatically on Themes tab
- [ ] Opening loot crate with theme reward → auto-switches
- [ ] Opening loot crate with collectible reward → auto-switches
- [ ] Manually clicking section buttons still works
- [ ] Cross-tab navigation works (click item while on different tab)

### User Experience Level
- [ ] No flickering when switching tabs
- [ ] Detail panels load correctly
- [ ] Search still works after tab switch
- [ ] Multiple clicks don't cause issues

## Performance Notes
- **No performance impact**: Just an extra ListBox.SelectedIndex assignment
- **No memory issues**: All methods are event-driven and cleanup on their own
- **No threading issues**: All operations on UI thread (as required)

## Debugging Tips
If things don't work as expected:

1. **Check if handlers are wired**: 
   - Open MainWindow.xaml
   - Verify `SelectionChanged="CollectiblesGrid_SelectionChanged"` exists
   - Verify `SelectionChanged="ThemesGrid_SelectionChanged"` exists

2. **Check if methods exist**:
   - Open MainWindow.xaml.cs
   - Search for `CollectiblesGrid_SelectionChanged`
   - Search for `ThemesGrid_SelectionChanged`
   - Search for `SelectCollectibleAndSwitchTab`
   - Search for `SelectThemeAndSwitchTab`

3. **Add debug breakpoints**:
   - Set breakpoint in CollectiblesGrid_SelectionChanged
   - Set breakpoint in SelectCollectibleAndSwitchTab
   - Click a collectible and check breakpoint hits

4. **Check BackRoomsSectionList is not null**:
   - Verify BackRoomsSectionList is properly named in XAML
   - Verify x:Name="BackRoomsSectionList" exists
