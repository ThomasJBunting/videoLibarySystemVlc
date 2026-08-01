# VLC Playback Position Tracking Implementation

## Overview
This implementation adds **real-time playback position tracking** by communicating with VLC's RC (Remote Control) interface. Now your app will track the actual playback position every 30 seconds, making resume functionality much more accurate.

---

## What Was Added

### 1. **VlcRcClient.cs** (NEW FILE)
- A TCP client that connects to VLC's RC interface
- Commands supported:
  - `get_time` - Gets current playback position in seconds
  - `get_length` - Gets total video length in seconds
- Auto-connects with 5-second timeout
- Handles connection failures gracefully

### 2. **VlcLauncher.cs** (UPDATED)
- Now launches VLC with `--rc-host=localhost:{port}` (random port 50000-50100)
- `VlcSession` class updated to:
  - Store the RC port
  - Provide `GetRcClientAsync()` to connect to VLC
  - Auto-dispose the RC client on cleanup

### 3. **MainWindow.xaml.cs** (UPDATED)

#### Three tracking methods:

**a) `TrackSeriesPlaybackFromVlcAsync`** (ENHANCED)
   - Polls window title every 1.2 seconds (detects episode changes)
   - **NEW:** Polls playback position via RC every 30 seconds
   - Updates `appState.Playback.LastKnownTimeSeconds` with actual position

**b) `TrackMoviePlaybackFromVlcAsync`** (NEW)
   - Polls playback position via RC every 30 seconds for movies
   - Updates `appState.Playback.LastKnownTimeSeconds`

**c) `PlayItemAsync` & `PlayWithXspfPlaylistAsync`** (UPDATED)
   - Now start tracking for BOTH movies and series
   - Uses RC interface for position tracking

---

## How It Works

### Flow Diagram:
```
1. User plays video
   └─> VLC launches with --rc-host=localhost:50XXX

2. Tracking starts (background loop)
   └─> Connects to RC interface (1.5s delay for VLC startup)

3. Every 30 seconds:
   └─> Sends "get_time" command to VLC
   └─> Receives current position (e.g., "1245" = 20:45 into video)
   └─> Updates appState.Playback.LastKnownTimeSeconds = 1245
   └─> Saves to JSON file

4. User closes VLC and reopens video
   └─> VLC launches with --start-time=1245
   └─> Resumes at 20 minutes 45 seconds
```

---

## Key Breakpoints for Testing

If you want to debug and see the tracking in action:

### In `TrackSeriesPlaybackFromVlcAsync` (MainWindow.xaml.cs):
- **Line ~1003**: When RC client connects
- **Line ~1012**: When position is polled every 30 seconds
- **Line ~1019**: When `LastKnownTimeSeconds` is updated

### In `TrackMoviePlaybackFromVlcAsync` (MainWindow.xaml.cs):
- **Line ~1100**: When RC client connects
- **Line ~1109**: When position is polled
- **Line ~1116**: When `LastKnownTimeSeconds` is updated

### In `VlcRcClient.cs`:
- **Line ~52**: `GetCurrentTimeSecondsAsync` - When position is queried
- **Line ~109**: `SendCommandAsync` - When command is sent to VLC
- **Line ~129**: `ReadResponseAsync` - When VLC responds

---

## What Gets Saved

### Before (Time Estimation):
```json
{
  "LastItemId": "ABC123",
  "LastFilePath": "C:\\Videos\\Episode1.mkv",
  "LastStartedUtc": "2024-01-15T14:30:00Z",
  "LastKnownTimeSeconds": null  ← ALWAYS NULL!
}
```

### After (Real Position):
```json
{
  "LastItemId": "ABC123",
  "LastFilePath": "C:\\Videos\\Episode1.mkv",
  "LastStartedUtc": "2024-01-15T14:30:00Z",
  "LastKnownTimeSeconds": 1245  ← REAL VALUE EVERY 30 SECONDS!
}
```

---

## Testing Instructions

1. **Start a video** (movie or series episode)
2. **Wait 30 seconds** - First position update happens
3. **Check the state file** (typically in `%LocalAppData%\VideoLibrarySystemVlc\appstate.json`)
4. **Look for `LastKnownTimeSeconds`** - Should be updated with real seconds
5. **Close VLC** (just the player window)
6. **Reopen the same video**
7. **Verify it resumes** at the tracked position (within ~30 seconds accuracy)

---

## Fallback Behavior

If RC connection fails:
- App continues to work normally
- Falls back to time estimation (current behavior)
- No errors shown to user
- Tracking loop continues checking

---

## Performance Impact

- **Minimal CPU usage**: Only polls every 30 seconds
- **Tiny network overhead**: Localhost TCP, ~50 bytes per poll
- **No UI blocking**: All RC calls are async
- **Graceful degradation**: If RC fails, app still works

---

## Future Enhancements (Optional)

- Add a UI indicator showing "Position tracked: 20:45"
- Allow user to configure poll interval (15s, 30s, 60s)
- Track watch history with timestamps
- Add "Skip Intro" feature using tracked positions
