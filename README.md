# AceSpectateMod
# Spectator Mod for Gorilla Tag

## Overview
A comprehensive spectator mod for Gorilla Tag that allows you to spectate other players in smooth third-person view with real-time nametags showing player names and FPS.

## Features

### Spectator System
- **Third-Person Spectating**: Smooth camera following with adjustable distance, height, and smoothing
- **Player List**: Easy-to-use GUI showing all players in the lobby
- **Quick Controls**: 
  - Press `J` to open/close the spectator GUI
  - Press `ESC` to instantly stop spectating and close GUI
- **Local Rig Hiding**: Automatically hides your own body to prevent blocking the view

### Camera Controls
- **Distance**: Adjust how far the camera sits from the target (1.5m - 12m)
- **Height**: Control camera height relative to the target (0m - 4m)
- **Follow Smoothing**: Adjust how smoothly the camera follows (1 - 20)
- **Rotation Smoothing**: Control camera rotation smoothness (1 - 20)
- **Field of View**: Customize camera FOV (30° - 100°)
- All settings are saved between game launches

### Nametag System (AceNameTags Integration)
- **Real-time FPS Display**: Shows each player's current FPS with color coding:
  - 🟢 Green: 87+ FPS (Excellent)
  - 🟡 Orange: 50-86 FPS (Good)
  - 🔴 Red: Below 50 FPS (Poor)
- **Player Names**: Shows display names above each player
- **Customizable**: 
  - Toggle nametags on/off
  - Adjust scale (0.05x - 1.0x)
  - Adjust height offset (0m - 2m)
  - **Facing Options**:
    - Face Camera: Nametags always face the spectator camera
    - Face Backwards: Nametags always face away (perfect for streaming/casting)
    - Face Player Forward: Nametags face the direction the player is looking

## Installation

1. **Install BepInEx** (if not already installed):
   - Download BepInEx for Gorilla Tag
   - Extract to your Gorilla Tag game folder

2. **Install the Mod**:
   - Place the `SpectatorMod.dll` file in the `BepInEx/plugins` folder
   - Launch the game

## Controls

| Key | Action |
|-----|--------|
| `J` | Toggle GUI on/off |
| `ESC` | Stop spectating and close GUI (when spectating) |
| Mouse Drag | Move GUI window by dragging the title bar |

## GUI Tabs

### Players Tab
- Lists all players currently in the lobby
- Click a player's name to start spectating them
- "Stop Spectating" button to return to normal view

### Settings Tab
- Adjust camera distance, height, smoothing, and FOV
- Reset all camera settings to defaults
- Settings are automatically saved

### Nametags Tab
- Toggle nametags on/off
- Adjust scale and height offset
- Choose facing direction:
  - Face Camera
  - Face Backwards (best for streaming)
  - Face Player Forward
- Reset all nametag settings to defaults


You can manually edit this file to adjust settings:
- Camera Distance
- Camera Height
- Follow Smoothness
- Rotation Smoothness
- Field of View
- Nametag Enabled
- Nametag Scale
- Nametag Height Offset
- Nametag Facing (0=Face Camera, 1=Face Backwards, 2=Face Player Forward)

## Technical Notes

### Camera System
- Uses a separate overlay camera (does not interfere with VR camera)
- Smooth interpolation for natural camera movement
- Camera renders on top of main game view

### Performance
- Optimized player list caching (refreshes every 0.5 seconds)
- Efficient nametag updating (only updates when name or FPS changes)
- Minimal performance impact

### Compatibility
- Works with both desktop and VR modes
- Compatible with other BepInEx mods
- No modification to game files required

## Troubleshooting

**GUI not opening?**
- Verify BepInEx is properly installed
- Check the console for error messages
- Make sure the mod .dll is in the correct plugins folder

**Nametags not showing?**
- Ensure nametags are enabled in the Nametags tab
- Verify you are spectating someone (nametags only show when spectating)
- Try refreshing the player list

**Camera not moving smoothly?**
- Adjust Follow Smoothness and Rotate Smoothness settings
- Lower values = faster response, higher values = smoother

**Performance issues?**
- Reduce nametag scale (smaller = better performance)
- Disable nametags if not needed
- Increase player list refresh interval (if you modify the code)

## Credits
- Original spectator system based on Gorilla Tag modding community
- Nametag system inspired by AceNameTags
- Built with BepInEx and Unity

## Version History
- **v1.1.1**: Added nametag facing options, ESC to stop spectating, and performance optimizations
- **v1.1.0**: Integrated AceNameTags system with FPS display
- **v1.0.0**: Initial release with spectating and basic settings

## Support
For issues or suggestions, please refer to the modding community forums or create an issue on the mod's repository.

---

**Enjoy spectating!** 🎮
## Configuration File
Settings are automatically saved to:
