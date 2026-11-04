# Back Rooms Navigation Issue - Code Review

## Issue Summary
When clicking on a collectible or theme item in the Back Rooms section, the UI should automatically navigate to the correct tab (Collectibles or Themes tab respectively). Currently, this doesn't happen.

## Current Behavior
1. **Collectibles List**: When you click on a Collectible tile, `SelectedCollectible` is set, which shows the detail panel, but the UI stays on whatever section tab was previously selected.
2. **Themes List**: Same issue - clicking a theme sets `SelectedTheme` but doesn't switch to the Themes tab.
3. **Navigation List**: The `BackRoomsSectionList` (Collectibles, Themes, Loot Crate Info) controls visibility of sections, but clicking items in the grids doesn't update this selection.

## Root Cause
The ListBoxes for collectibles and themes in the XAML are bound to:
```xaml
<ListBox x:Name="BackRoomsSectionList" 
		 SelectedIndex="0"
		 Height="200"
		 SelectionChanged="BackRoomsSectionList_SelectionChanged">
	<ListBoxItem Content="Collectibles" />
	<ListBoxItem Content="Themes" />
	<ListBoxItem Content="Loot Crate Info" />
</ListBox>
```

And the collapsible sections use:
```xaml
<Grid x:Name="CollectiblesSection" Visibility="Visible">
<Grid x:Name="ThemesSection" Visibility="Collapsed">
<Grid x:Name="LootCrateInfoSection" Visibility="Collapsed">
```

The `BackRoomsSectionList_SelectionChanged` handler toggles visibility:
```csharp
CollectiblesSection.Visibility = selectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
ThemesSection.Visibility = selectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
LootCrateInfoSection.Visibility = selectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
```

However, when a collectible or theme is clicked in their respective grids, the `BackRoomsSectionList.SelectedIndex` is never updated to match.

## Required Fixes

### 1. Add Method to MainWindow.xaml.cs
```csharp
/// <summary>
/// Switch to the Collectibles section and select the specified collectible.
/// </summary>
private void SelectCollectible(Collectible collectible)
{
	if (BackRoomsSectionList != null)
	{
		BackRoomsSectionList.SelectedIndex = 0; // Collectibles tab
	}
	SelectedCollectible = collectible;
}

/// <summary>
/// Switch to the Themes section and select the specified theme.
/// </summary>
private void SelectThemeFromCrate(ThemeDefinition theme)
{
	if (BackRoomsSectionList != null)
	{
		BackRoomsSectionList.SelectedIndex = 1; // Themes tab
	}
	SelectedTheme = theme;
}
```

### 2. Hook Up Event Handlers in XAML
The collectibles and themes ListBoxes need to handle selection changes and call these methods:

For Collectibles ListBox:
```xaml
<ListBox Grid.Row="2"
		 ItemsSource="{Binding BackRoomsCollectibles}" 
		 SelectedItem="{Binding SelectedCollectible}"
		 SelectionChanged="CollectiblesGrid_SelectionChanged"
		 ...>
```

For Themes ListBox:
```xaml
<ListBox Grid.Row="1"
		 ItemsSource="{Binding AvailableThemesList}" 
		 SelectedItem="{Binding SelectedTheme}"
		 SelectionChanged="ThemesGrid_SelectionChanged"
		 ...>
```

### 3. Add Event Handlers in Code-Behind
```csharp
private void CollectiblesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
	if (e.AddedItems.Count > 0 && e.AddedItems[0] is Collectible collectible)
	{
		SelectCollectible(collectible);
	}
}

private void ThemesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
	if (e.AddedItems.Count > 0 && e.AddedItems[0] is ThemeDefinition theme)
	{
		SelectThemeFromCrate(theme);
	}
}
```

## Expected After Fix
✅ Click on Collectible → Auto-switches to "Collectibles" tab and shows details
✅ Click on Theme → Auto-switches to "Themes" tab and shows details
✅ Navigation list always reflects which section you're viewing
✅ Clicking from any section button changes the view (already works)

## Testing with Themes JSON
To test this fix with your `themes_expanded.json`:
1. Load the themes file into the system
2. Click "Open Loot Crate" to get themes
3. When themes appear in the Grid, clicking them should:
   - Switch to the Themes section automatically
   - Select the clicked theme
   - Show theme details in the detail panel

Same workflow for collectibles.
