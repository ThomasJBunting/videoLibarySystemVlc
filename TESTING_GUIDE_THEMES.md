# Testing Back Rooms Navigation with themes_expanded.json

## Quick Start Test

### Setup
1. **Copy your theme files** to the project:
   - Place `themes_expanded.json` in the JSON folder (alongside collectibles.json)
   - Or note its path for loading

2. **Reload data in application**:
   - Open Back Rooms tab
   - Click "🔄 Reload Data" button to load themes

3. **Verify themes loaded**:
   - Click "Themes" section button
   - Should see 16 theme tiles (1 Tie-Dye + 15 country-themed)

### Test Scenario 1: Theme Selection
```
Expected Behavior:
1. You're on "Collectibles" tab
2. Click "Themes" section button → Switch to Themes tab
3. See grid of themes: "Groovy Tie-Dye", "Dojo Discipline", etc.
4. Click any theme card
   ► Should reflect correct visual theme (colors change)
   ► Should show theme details below
   ► Should show theme in detail panel
   ✅ TEST PASSES if:
	  - Tab stays on "Themes"
	  - Detail panel shows selected theme info
	  - No error messages
```

### Test Scenario 2: Loot Crate with Theme Reward
```
Setup:
1. Ensure you have gems (edit gem count to > 1 if needed)
2. Load themes via "🔄 Reload Data"

Steps:
1. Click "Open Loot Crate" button
2. If you get a theme:
3. Toast notification shows theme unlocked
4. Loot crate opens
5. Shows theme name and details

Expected Result:
✅ TEST PASSES if:
   - UI automatically switches to "Themes" tab
   - Newly unlocked theme is selected
   - Detail panel shows the new theme
   - Theme is visible in the theme grid
```

### Test Scenario 3: Cross-Tab Navigation
```
Setup:
1. Have both collectibles and themes loaded
2. Be on "Collectibles" tab

Steps:
1. Scroll to see at least one theme card (if visible)
   OR go to Themes, then back to Collectibles
2. Click on a collectible card
3. Verify Collectibles tab is active and item shows
4. Click "Themes" section button
5. Click on a theme card

Expected Result:
✅ TEST PASSES if:
   - After step 3: Collectibles tab active, collectible details show
   - After step 5: Themes tab active, theme details show
   - No visual glitches or errors
   - Tab switching is smooth
```

## Detailed Test Cases

### TC-1: Verify All 16 Themes Load

**Steps:**
1. Click "🔄 Reload Data" button in Back Rooms
2. Click "Themes" section button
3. Count theme tiles in grid

**Expected Results:**
| Theme Name | Icon | Verify Visible |
|-----------|------|-----------------|
| Groovy Tie-Dye | 🌀 | ☐ |
| Dojo Discipline | 🥋 | ☐ |
| Venetian Carnival | 🎭 | ☐ |
| Highland Rebellion | 🏰 | ☐ |
| Shaolin Temple | ⛩️ | ☐ |
| Jungle Recon | 🌿 | ☐ |
| Neo-Apocalypse | 🤖 | ☐ |
| Eternal Darkness | 🧛 | ☐ |
| Steel City Dreams | 🥊 | ☐ |
| Power Nexus | ⚡ | ☐ |
| Underground Mutants | 🐢 | ☐ |
| Neverland Enchantment | ✨ | ☐ |
| Bayou Refuge | 🦗 | ☐ |
| Sith Dominion | ⚫ | ☐ |
| Ancient Artifacts | 🏺 | ☐ |
| Stark Industrial | ⚙️ | ☐ |
| Inferno Mutant | 🔥 | ☐ |

**Pass/Fail:** All 16 themes visible ☐ Pass ☐ Fail

---

### TC-2: Theme Details Display

**Steps:**
1. Go to "Themes" section
2. Click "Dojo Discipline" (Japan, 🥋)

**Expected Result - Detail Panel Shows:**
- Name: "Dojo Discipline"
- Icon: 🥋
- Country/Region: "Japan"
- Description: "Master the way of the warrior..."
- Rarity: "Uncommon"
- Drop Weight: 22

**Pass/Fail:** ☐ Pass ☐ Fail

**Repeat for:** "Sith Dominion" (Epic rarity, different colors)

---

### TC-3: Navigation from Collectibles Section

**Steps:**
1. Click "Collectibles" section
2. See collectibles in grid
3. Click "Highland Rebellion" theme card (if visible in grid)
4. Observe tab switch

**Expected Result:**
- Themes tab automatically activates
- "Highland Rebellion" is selected
- Detail panel shows Scottish theme info
- Description mentions "tartan, bagpipes, and ancient warrior pride"

**Pass/Fail:** ☐ Pass ☐ Fail

---

### TC-4: Rapid Tab Switching

**Steps:**
1. Click "Collectibles" → Click a collectible
2. Click "Themes" → Click a theme
3. Click "Collectibles" → Click another collectible
4. Repeat 3 times

**Expected Result:**
- All tab switches instant and smooth
- No lag or visual artifacts
- Correct details show each time
- No error messages

**Pass/Fail:** ☐ Pass ☐ Fail

---

### TC-5: Search Box Works After Tab Switch

**Steps:**
1. Click "Themes" section
2. Click "Neo-Apocalypse" theme
3. Click search icon (🔍) to show search box
4. Type "Cyber" in search box
5. Verify results

**Expected Result:**
- Search box appears
- Filtering works correctly
- Can still see "Neo-Apocalypse" selected in detail panel

**Pass/Fail:** ☐ Pass ☐ Fail

---

### TC-6: Theme Application

**Steps:**
1. Go to Themes section
2. Click "Groovy Tie-Dye" (the 1970s theme)
3. Click "Apply This Theme" button
4. Observe application

**Expected Result:**
- UI colors change to psychedelic theme
- Swirling colors: gold, pink, green, blue
- Background becomes darker (#2F4F4F - dark slate)
- Text remains readable

**Pass/Fail:** ☐ Pass ☐ Fail

**Repeat for:** "Sith Dominion" (dark red/black theme)

---

### TC-7: Loot Crate Animation with Themes

**Setup:**
1. Ensure gems ≥ 1
2. Load themes via refresh
3. Clear any existing selections

**Steps:**
1. Click "Open Loot Crate"
2. Wait for crate animation
3. Observe result

**Expected Outcomes (Any is Valid):**
- **If Collectible**: Auto-switches to Collectibles, shows item
- **If Theme**: Auto-switches to Themes, shows theme

**Pass/Fail:** ☐ Pass ☐ Fail

---

## Color Verification Test

### Tie-Dye Theme Colors
```
Primary Colors:
  - #FF1493 (Hot Pink)
  - #00FF00 (Lime Green)
  - #FFD700 (Gold)
  - #FF6347 (Tomato Red)
  - #0000FF (Blue)

Secondary:
  - #9932CC (Dark Orchid)
  - #FF8C00 (Orange)
  - #00FFFF (Cyan)

Background: #2F4F4F (Dark Slate Gray)
Accent: #FFD700 (Gold)
```

**Test:** Apply theme, verify colors match theme palette

---

### Sith Domain Colors
```
Primary:
  - #000000 (Black)
  - #FF0000 (Red)
  - #8B0000 (Dark Red)

Secondary:
  - #4B0082 (Indigo)
  - #696969 (Dim Gray)

Background: #0a0a0a (Nearly black)
Accent: #FF0000 (Red)
```

**Test:** Apply theme, UI appears dark with red accents

---

## Common Issues & Troubleshooting

### Issue 1: Themes Don't Load
**Solution:**
1. Verify JSON file path is correct
2. Check JSON format is valid (match existing collectibles.json structure)
3. Open "🔄 Reload Data" button
4. Check browser DevTools for errors (F12)

### Issue 2: Tab Doesn't Switch When Clicking Theme
**Solution:**
1. Verify you're clicking on the theme CARD (not empty space)
2. Check if SelectionChanged event fires (add breakpoint)
3. Verify MainWindow.xaml has `SelectionChanged="ThemesGrid_SelectionChanged"`
4. Check MainWindow.xaml.cs has `ThemesGrid_SelectionChanged` method

### Issue 3: Detail Panel Shows Wrong Theme
**Solution:**
1. Verify SelectedTheme binding is correct
2. Check if multiple themes have same ID
3. Restart application to clear cache
4. Reload data

### Issue 4: Colors Don't Apply
**Solution:**
1. Verify theme has themeData with color values
2. Check if colors are valid hex (#RRGGBB)
3. Verify ThemeManager is reading themeData correctly
4. Try applying a built-in theme to verify system works

---

## Automated Testing (For Dev)

### Unit Tests to Write
```
[Test] ThemesGrid_SelectionChanged_WithTheme_SwitchesTab()
  - Create ThemeDefinition mock
  - Set e.AddedItems with mock
  - Call handler
  - Assert BackRoomsSectionList.SelectedIndex == 1

[Test] SelectThemeAndSwitchTab_WithNullList_DoesNotCrash()
  - Set BackRoomsSectionList = null
  - Call method with theme
  - Assert no exception thrown
  - Assert SelectedTheme set correctly

[Test] CollectiblesGrid_SelectionChanged_WithCollectible_SwitchesTab()
  - Create Collectible mock
  - Set e.AddedItems with mock
  - Call handler
  - Assert BackRoomsSectionList.SelectedIndex == 0
```

---

## Success Checklist

- [x] All 16 themes load correctly
- [ ] Clicking theme auto-switches to Themes tab
- [ ] Clicking collectible auto-switches to Collectibles tab
- [ ] Detail panels show correct info
- [ ] Theme application works with new themes
- [ ] Search still works after tab switch
- [ ] Loot crate opening shows correct section
- [ ] No error messages or warnings
- [ ] No memory leaks (Task Manager shows stable memory)
- [ ] Rapid clicking doesn't cause lag
- [ ] Theme colors appear correct when applied
- [ ] Backward compatible (old collectibles still work)

**Overall Status:** ☐ Ready for Production ☐ Needs Adjustment

---

## Final Sign-Off

**Tester:** ___________________  
**Date:** ___________________  
**Notes:** 
```
[Space for additional observations]
```
