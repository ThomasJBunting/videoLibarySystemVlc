# Back Rooms (Video Rental Desk) Feature

## Overview
The Back Rooms tab is a fun, retro video rental store-themed collectibles system with:
- **Gem Economy**: Earn 1 gem every 24 hours (or cheat by editing the counter!)
- **Loot Crates**: Open crates for 1 gem each, win random collectibles
- **Collectibles Gallery**: Pokemon card-sized display of your won items
- **Ticker Tape**: Scrolling user reviews at the bottom of the window
- **Pay Your Late Fee**: Easter egg button that redirects to a URL

## Getting Started

### 1. Configure the Back Rooms
Go to the **Settings** tab and scroll down to "Back Rooms Settings":

**Collectibles Source URL:**
```
file:///C:/Users/bunti/source/repos/videoLibarySystemVlc/SampleData/collectibles.json
```
(Or use your full path to the SampleData/collectibles.json file)

**Ticker Tape Reviews URL:**
```
file:///C:/Users/bunti/source/repos/videoLibarySystemVlc/SampleData/reviews.json
```

**Late Fee URL:** (Default: Google, change to anything fun!)
```
https://www.google.com
```

Click the **Save** buttons after entering each URL.

### 2. Test the Features

#### Gem System
- Check the gem counter on the **Back Rooms** tab
- Wait 24 hours for a gem (or...)
- **Cheat**: Click the gem counter and type in any number (e.g., 999)!

#### Open Loot Crates
1. Make sure you have at least 1 gem
2. Click **"Open Loot Crate"**
3. You'll win a random collectible based on drop weights
4. Each rarity has different chances:
   - Common (30-25 weight): ~30-40% chance
   - Uncommon (18-12 weight): ~15-20% chance
   - Rare (10-8 weight): ~10-12% chance
   - Epic (5 weight): ~5% chance
   - Legendary (2 weight): ~2% chance

#### View Collectibles
- Switch to the **"Collectibles"** section in the left panel
- Your won items appear as Pokemon card-sized tiles (250x380)
- Click any collectible to see details in the panel below

#### Loot Crate Info
- Switch to **"Loot Crate Info"** section
- See how the system works
- View available collectibles count

#### Ticker Tape
- The ticker tape scrolls at the bottom of the window
- Shows reviews in format: "Name: Review text  •  Name: Review text"
- Toggle it on/off in Settings → "Enable Ticker Tape"

#### Pay Your Late Fee
- Click the **"Pay Your Late Fee"** button
- Opens your configured URL in the browser
- (Eventually this could link to a review submission form!)

## Data Storage

### User Data Location
```
%AppData%\VideoLibrarySystemVlc\backrooms.json
```
This file contains:
- Your gem count
- Last gem award time
- All collected items

### Collectible Images Cache
```
%AppData%\VideoLibrarySystemVlc\collectibles\
```
Images are downloaded here when you win them.

## JSON Format Examples

### Collectibles Source (collectibles.json)
```json
[
  {
	"id": "unique-id",
	"name": "Collectible Name",
	"description": "Fun description text",
	"imageUrl": "https://example.com/image.jpg",
	"dropWeight": 10,
	"rarity": "Rare"
  }
]
```

**Drop Weight**: Higher numbers = more common
- 30: Very Common (~30%)
- 20: Common (~20%)
- 10: Uncommon (~10%)
- 5: Rare (~5%)
- 2: Very Rare (~2%)

### Reviews Source (reviews.json)
```json
[
  {
	"name": "Username",
	"reviewText": "Short review (5-7 words)",
	"submittedDateUtc": "2024-01-15T10:30:00Z",
	"mediaTitle": "Movie Name"
  }
]
```

## Tips & Tricks

1. **Test with Local Files First**: Use `file:///` URLs to test with the sample data before hosting remotely
2. **Cheat the System**: The gem counter is editable - type 999 for unlimited crates!
3. **Remote Hosting**: You can host collectibles.json and reviews.json on GitHub, Pastebin, etc.
4. **Image URLs**: Use any image URL (Unsplash, Imgur, etc.) for collectible artwork
5. **Ticker Tape**: Reviews load from the JSON, so you can update them without restarting

## Roadmap (Future Ideas)

- [ ] Duplicate detection (prevent winning the same collectible twice)
- [ ] Trade system (export/import collectibles)
- [ ] Achievements system
- [ ] Seasonal/event collectibles
- [ ] Collectible star ratings or favorites
- [ ] Review submission form (integrated with "Pay Late Fee")
- [ ] Statistics (total crates opened, rarest item, etc.)

Enjoy your retro video rental desk experience! 🎬📼💎
