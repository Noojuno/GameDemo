namespace GameDemo;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class PlayerManager : Node3D
{
  public override void _Notification(int what) => this.Notify(what);

  #region Dependencies

  [Dependency]
  public EntityTable EntityTable => this.DependOn<EntityTable>();

  [Dependency]
  public IMultiplayerRepo MultiplayerRepo => this.DependOn<IMultiplayerRepo>();

  #endregion Dependencies

  #region Nodes

  [Node("%PauseContainer")] public Node3D PauseContainer { get; set; } = default!;

  #endregion Nodes

  private readonly Dictionary<int, FirstPersonPlayer> _remotePlayers = new();

  private static PackedScene PlayerScene => GD.Load<PackedScene>("res://src/player/Player.tscn");

  public void Setup()
  {
  }

  public void OnReady()
  {
    MultiplayerRepo.PeerConnected += OnPeerConnected;
    MultiplayerRepo.PeerDisconnected += OnPeerDisconnected;
    MultiplayerRepo.IsOnline.Sync += OnIsOnlineChanged;
  }

  public void OnResolved()
  {
    EnsureLocalPlayerAuthority();

    foreach (var peer in MultiplayerRepo.Peers.Values)
    {
      if (peer.PeerId != MultiplayerRepo.LocalPeerId.Value)
      {
        CreateRemotePlayer(peer.PeerId);
      }
    }

    if (Multiplayer.HasMultiplayerPeer() && Multiplayer.IsServer())
    {
      foreach (var peer in MultiplayerRepo.Peers.Values)
      {
        if (peer.PeerId != MultiplayerRepo.LocalPeerId.Value)
        {
          Rpc(MethodName.CreatePlayerForPeer, peer.PeerId);
        }
      }
    }
  }

  private void OnIsOnlineChanged(bool isOnline)
  {
    if (isOnline)
    {
      EnsureLocalPlayerAuthority();
    }
  }

  private void OnPeerConnected(int peerId)
  {
    if (peerId == MultiplayerRepo.LocalPeerId.Value)
    { return; }

    CreateRemotePlayer(peerId);

    if (Multiplayer.IsServer())
    {
      // Tell everyone to create the new peer's player.
      Rpc(MethodName.CreatePlayerForPeer, peerId);

      // Tell the new peer to create players for existing peers.
      foreach (var existingPeer in MultiplayerRepo.Peers.Values)
      {
        if (existingPeer.PeerId != peerId && existingPeer.PeerId != MultiplayerRepo.LocalPeerId.Value)
        {
          RpcId(peerId, MethodName.CreatePlayerForPeer, existingPeer.PeerId);
        }
      }
    }
  }

  private void OnPeerDisconnected(int peerId)
  {
    if (_remotePlayers.TryGetValue(peerId, out var player))
    {
      player.QueueFree();
      _remotePlayers.Remove(peerId);
    }
  }

  private void CreateRemotePlayer(int peerId)
  {
    if (_remotePlayers.ContainsKey(peerId))
    { return; }

    var player = PlayerScene.Instantiate<FirstPersonPlayer>();
    player.Name = $"Player_{peerId}";

    // Ensure remote player is part of the paused content tree.
    PauseContainer.AddChild(player);

    // Assign network authority to the owning peer so input/physics run only there.
    player.SetMultiplayerAuthority(peerId, true);

    // Ensure network authority state and visuals are correct after entering the tree.
    player.CallDeferred(nameof(FirstPersonPlayer.RefreshNetworkAuthority));

    _remotePlayers[peerId] = player;
  }

  public void OnExitTree()
  {
    MultiplayerRepo.PeerConnected -= OnPeerConnected;
    MultiplayerRepo.PeerDisconnected -= OnPeerDisconnected;
    MultiplayerRepo.IsOnline.Sync -= OnIsOnlineChanged;
  }

  private void EnsureLocalPlayerAuthority()
  {
    var player = EntityTable.Get<IPlayer>("Player") as Node;
    if (player is null)
    { return; }

    var localPeerId = MultiplayerRepo.LocalPeerId.Value;
    if (player.GetMultiplayerAuthority() != localPeerId)
    {
      player.SetMultiplayerAuthority(localPeerId, true);
      if (player is FirstPersonPlayer fp)
      {
        fp.RefreshNetworkAuthority();
      }
    }
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CreatePlayerForPeer(int peerId)
  {
    CreateRemotePlayer(peerId);
  }
}


