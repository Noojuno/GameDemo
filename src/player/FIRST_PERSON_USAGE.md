# First-Person Controller Usage Guide

This guide explains how to use the first-person player controller and camera system.

## Overview

The first-person controller system consists of:
- **FirstPersonPlayer.cs** - First-person character controller using LogicBlocks
- **FirstPersonCamera.cs** - First-person camera controller using LogicBlocks

Both systems are drop-in replacements for the third-person versions and maintain compatibility with the existing game systems.

## Scene Setup

### Option 1: Use the Provided FirstPersonGame Scene

The easiest way to use first-person mode is to use the provided `FirstPersonGame.tscn` scene:

1. Open `src/game/FirstPersonGame.tscn` in Godot
2. This scene already has FirstPersonPlayer and FirstPersonCamera configured
3. Update your App.cs to use this scene instead of Game.tscn (or create a separate entry point)

### Option 2: Modify Existing Game Scene

To switch your existing Game.tscn to first-person:

1. **Replace PlayerCamera**:
   - In Game.tscn, replace the PlayerCamera instance with FirstPersonCamera.tscn
   - Path: `res://src/player_camera/FirstPersonCamera.tscn`

2. **Player Scene**:
   - The Player.tscn already uses FirstPersonPlayer.cs
   - Optionally hide the PlayerModel node for true first-person (no visible character model)

3. **Camera Offset**:
   - Set the camera Offset to `(0, 1.6, 0)` for standard eye height
   - Adjust as needed for your character

## Features

### First-Person Player Controller

- **Camera-relative movement**: Movement is relative to camera direction (no character rotation)
- **Full LogicBlocks integration**: Uses the same state machine pattern as third-person
- **Coin collection**: Fully compatible with existing coin collection system
- **All interfaces preserved**: IKillable, IPushEnabled, ICoinCollector all work

### First-Person Camera

- **Mouse and joystick support**: Handles both input methods
- **Wide vertical range**: Full vertical look range (-89.9° to 89.9°)
- **Smooth following**: Camera follows player position smoothly
- **No spring arm**: Direct attachment to player for first-person feel

## Configuration

### FirstPersonCameraSettings

Edit the Settings resource in FirstPersonCamera.tscn to adjust:

- **MouseSensitivity**: Mouse look sensitivity (default: 0.2)
- **JoypadSensitivity**: Joystick look sensitivity (default: 5.0)
- **VerticalMax/Min**: Vertical look limits (default: -89.9° to 89.9°)
- **FollowSpeed**: How fast camera follows player (default: 20.0)
- **RotationAcceleration**: Smooth rotation speed (default: 10.0)

### FirstPersonPlayer Exports

The FirstPersonPlayer has the same exports as the regular Player:

- **StoppingSpeed**: Velocity threshold for stopping (default: 1.0)
- **Gravity**: Gravity strength (default: -30.0)
- **MoveSpeed**: Movement speed (default: 8.0)
- **Acceleration**: Movement acceleration (default: 4.0)
- **JumpImpulseForce**: Initial jump force (default: 12.0)
- **JumpForce**: Additional jump force while holding jump (default: 4.5)

## Differences from Third-Person

1. **No character rotation**: The character body doesn't rotate - movement is camera-relative
2. **No spring arm**: Camera attaches directly to player position
3. **No lateral offset**: No strafe offset (not needed for first-person)
4. **Wider vertical range**: Full vertical look range instead of limited third-person range

## Integration with Existing Systems

The first-person controller is fully compatible with:

- ✅ Coin collection system
- ✅ Save/load system
- ✅ Death/respawn system
- ✅ Pause menu
- ✅ Game repository (position tracking, camera basis)
- ✅ All existing game logic

## Troubleshooting

### Camera doesn't follow player
- Ensure the camera's `Offset` is set correctly
- Check that `GameRepo.SetPlayerGlobalPosition()` is being called (handled automatically)

### Movement feels wrong
- Verify camera basis is being updated (handled automatically by camera)
- Check that input actions are configured correctly

### Character model visible in first-person
- Hide or remove the PlayerModel node in Player.tscn
- Or set it to invisible in the scene

## Example: Creating a New First-Person Scene

```gdscript
# In Godot, create a new scene:
# 1. Add Node3D as root (name it "FirstPersonGame")
# 2. Add FirstPersonCamera.tscn as child
# 3. Add Player.tscn as child  
# 4. Add Map.tscn as child
# 5. Add UI elements (InGameUI, etc.)
```

The scene structure should match Game.tscn but with FirstPersonCamera instead of PlayerCamera.

