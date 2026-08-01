# Quick Test Instructions

## Step 1: Configure URLs in Settings

1. Run the app
2. Go to **Settings** tab
3. Scroll down to "Back Rooms Settings"
4. Copy and paste these URLs (adjust the path to match your computer):

**Collectibles Source URL:**
```
file:///C:/Users/bunti/source/repos/videoLibarySystemVlc/SampleData/collectibles.json
```

**Ticker Tape Reviews URL:**
```
file:///C:/Users/bunti/source/repos/videoLibarySystemVlc/SampleData/reviews.json
```

5. Click **"Save Collectibles URL"** button
6. Click **"Save Ticker Reviews URL"** button
7. You should see status messages at the bottom confirming the saves

## Step 2: Go to Back Rooms Tab

1. Click the **Back Rooms** tab
2. You should see:
   - Gem counter (looks like plain text, click to edit it)
   - "Next gem in" timer
   - "Open Loot Crate" button
   - "🔄 Reload Data" button (for testing)

## Step 3: Test the System

1. **Click the gem counter** and type **10** (or any number)
2. Press Enter or click away
3. **Click "🔄 Reload Data"** button - this will load the collectibles
4. Check the status bar at the bottom - it should say how many collectibles loaded
5. **Click "Open Loot Crate"** button
6. You should see a popup showing what you won!

## Troubleshooting

If it's not working:

1. **Check the Status Bar** (bottom of window) for error messages
2. **Click "🔄 Reload Data"** and watch the status bar
3. **Check the file paths** - make sure they point to the actual location of the JSON files
4. The gem counter now looks like plain text but you can still click and edit it!

## What Changed

### Gem Counter Styling
- Now appears as **plain text** with no visible border or background
- Shows a subtle highlight only when you hover over it
- Click anywhere on the number to edit it
- Type any number you want (999 for unlimited crates!)

### Debug Features
- Added **"🔄 Reload Data"** button to manually reload collectibles without restarting
- Error messages now show more details about what went wrong
- Status bar shows exactly what's happening

### Test Data
- Now using **real Pokémon artwork** from the PokeAPI sprite repository
- 12 Pokémon cards: Bulbasaur, Charmander, Squirtle, Pikachu, Snorlax, Gengar, Mewtwo, Charizard, Gyarados, Dragonite, Lugia, Lucario
- Each has different rarities (Common, Uncommon, Rare, Epic, Legendary)
- Drop weights adjusted for testing (Starter Pokémon are most common, Lugia is legendary)
- Images download automatically from GitHub (reliable and free)

## Expected Results

When you open a loot crate:
- Your gem count goes down by 1
- A popup shows the collectible you won
- The collectible appears in the gallery (switch to "Collectibles" section)
- Click the collectible to see its details at the bottom

Enjoy! 🎮💎
