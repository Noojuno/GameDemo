# Multiplayer Implementation

This document describes the multiplayer implementation for the GameDemo project, which uses the existing Chickensoft architecture (LogicBlocks, AutoInject, Repositories).

## Architecture Overview

The multiplayer system follows the established patterns in the codebase:

### Core Components

1. **MultiplayerRepo** (`src/multiplayer/domain/MultiplayerRepo.cs`)
   - Stores multiplayer state (connection status, peer list, network mode)
   - Implements `IMultiplayerRepo` interface
   - Uses `AutoProp` for reactive state management

2. **MultiplayerLogic** (`src/multiplayer/state/MultiplayerLogic.cs`)
   - LogicBlock managing network states: `Offline`, `Hosting`, `Client`
   - Handles state transitions based on connection events
   - Outputs network status changes

3. **MultiplayerManager** (`src/multiplayer/MultiplayerManager.cs`)
   - Godot node that bridges Godot's multiplayer APIs with our architecture
   - Provides `IMultiplayerRepo` and `IMultiplayerLogic` via AutoInject
   - Sets up `ENetMultiplayerPeer` for networking
   - Handles peer registration and RPCs

4. **MultiplayerMenu** (`src/multiplayer_menu/MultiplayerMenu.cs`)
   - UI for hosting/joining games
   - Displays connection status and player list
   - Accessible via the 'M' key in-game

## Features

### Player Synchronization

- **Position & Velocity**: Synchronized every 50ms using unreliable RPCs
- **Authority**: Only the player with network authority processes input
- **Visual Model**: Hidden for local player (first-person), visible for remote players
- **Location**: `src/player/FirstPersonPlayer.cs`

### Coin Synchronization

- **Collection**: Server-authoritative coin collection
- **Reliability**: Uses reliable RPCs to ensure all clients see collected coins
- **Prevention**: Coins can only be collected once
- **Location**: `src/coin/Coin.cs`

### Game State

- **Coins**: Each player's coin collection is tracked through the existing `GameRepo`
- **Events**: Coin collection events flow through `GameRepo.StartCoinCollection()` and `GameRepo.OnFinishCoinCollection()`

## Usage

### Starting a Multiplayer Session

#### Host a Game

1. Run the game
2. Press **M** to open the multiplayer menu
3. Set the port (default: 7777)
4. Click **Host Game**
5. Other players can now join your game

#### Join a Game

1. Run the game
2. Press **M** to open the multiplayer menu
3. Enter the host's IP address (default: 127.0.0.1 for localhost)
4. Set the port (default: 7777)
5. Click **Join Game**

### Controls

- **M Key**: Toggle multiplayer menu
- **ESC**: Pause menu (existing)

## Network Architecture

### State Flow

```
Player Input → PlayerLogic → Node Updates → Network Sync (RPC)
                                                    ↓
Remote Client ← Node Updates ← RPC Received ← Network
```

### Peer Management

1. **Host Starts**: Server peer created, peer ID = 1
2. **Client Connects**: Client peer created, peer ID assigned by server
3. **Registration**: Peers register with `MultiplayerManager` via RPCs
4. **Disconnection**: Automatic cleanup when peers disconnect

## Technical Details

### Network Topology

- **Type**: Client-Server (Host acts as server)
- **Protocol**: ENet (UDP-based, reliable/unreliable channels)
- **Synchronization**: Authority-based (server/host is authoritative)

### RPC Usage

#### Player State Sync (Unreliable)
```csharp
[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, 
     TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
private void ReceiveNetworkState(Vector3 position, Vector3 velocity)
```

#### Coin Collection (Reliable)
```csharp
[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, 
     TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
private void StartCollectionRemote(string collectorName)
```

### Authority Model

- **Players**: Each player has authority over their own character
- **Coins**: Server has authority over coin collection
- **Game State**: Server manages authoritative game state

## Integration Points

### Game.cs

The `Game` node provides `IMultiplayerRepo` and `IMultiplayerLogic`:

```csharp
IMultiplayerRepo IProvide<IMultiplayerRepo>.Value() 
    => MultiplayerManager.MultiplayerRepo;
IMultiplayerLogic IProvide<IMultiplayerLogic>.Value() 
    => MultiplayerManager.MultiplayerLogic;
```

### Player

The `FirstPersonPlayer` depends on `IMultiplayerRepo`:

```csharp
[Dependency]
public IMultiplayerRepo MultiplayerRepo => this.DependOn<IMultiplayerRepo>();
```

### Coin

The `Coin` depends on `IMultiplayerRepo`:

```csharp
[Dependency]
public IMultiplayerRepo MultiplayerRepo => this.DependOn<IMultiplayerRepo>();
```

## Future Enhancements

Possible improvements for the multiplayer system:

1. **Player Spawning**: Dynamic player spawning/despawning when peers join/leave
2. **Animation Sync**: Synchronize player animations (walk, run, jump)
3. **Interpolation**: Add client-side prediction and interpolation for smoother movement
4. **Name Tags**: Display player names above remote players
5. **Voice Chat**: Integrate voice communication
6. **Matchmaking**: Add lobby system for finding games
7. **Latency Display**: Show ping/latency to server
8. **NAT Traversal**: Add UPnP or hole-punching for easier connections
9. **Save/Load**: Extend save system to work in multiplayer
10. **Chat System**: Text-based communication between players

## Troubleshooting

### Cannot Connect

- Ensure the host's firewall allows the port (default 7777)
- Verify the IP address is correct
- Check that both players are on the same network (for LAN) or use port forwarding (for WAN)

### Players Not Visible

- This is expected behavior in first-person mode for your own player
- Remote players should have visible models
- Check that the `PlayerModelNode.Visible` is set correctly based on authority

### Coins Not Syncing

- Verify the server/host has authority over coins
- Ensure RPCs are being called (check GD.Print statements)
- Check network connectivity

### Desynced State

- The server/host is authoritative for game state
- Clients receive updates from the server
- If desync occurs, it may indicate packet loss or network issues

## Architecture Benefits

This implementation maintains the existing architecture:

- **Separation of Concerns**: Logic in LogicBlocks, state in Repos, presentation in Nodes
- **Testability**: MultiplayerLogic and MultiplayerRepo can be tested independently
- **Dependency Injection**: Uses AutoInject for clean dependency management
- **Reactivity**: Uses AutoProp for reactive state updates
- **Consistency**: Follows the same patterns as existing game systems

