namespace GameDemo;

using System.Collections.Generic;
using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

[Meta(typeof(IAutoNode))]
public partial class CollectorProxyManager : Node3D
{
  public override void _Notification(int what) => this.Notify(what);

  #region Dependencies

  [Dependency]
  public EntityTable EntityTable => this.DependOn<EntityTable>();

  [Dependency]
  public IMultiplayerRepo MultiplayerRepo => this.DependOn<IMultiplayerRepo>();

  // We resolve the local player from the EntityTable when needed to avoid
  // depending on a provider that may not be available yet.

  #endregion Dependencies

  private readonly Dictionary<int, CollectorProxy> _proxies = new();
  private float _syncTimer = 0f;
  private const float SYNC_INTERVAL = 0.05f;

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
    SetPhysicsProcess(Multiplayer.HasMultiplayerPeer());

    if (Multiplayer.HasMultiplayerPeer())
    {
      CreateProxyForLocalPlayer();

      if (Multiplayer.IsServer())
      {
        var player = GetPlayer();
        if (player != null)
        {
          foreach (var peer in MultiplayerRepo.Peers.Values)
          {
            if (peer.PeerId != MultiplayerRepo.LocalPeerId.Value)
            {
              RpcId(peer.PeerId, MethodName.CreateProxyForPeer, MultiplayerRepo.LocalPeerId.Value, player.GlobalPosition);
            }
          }
        }
      }
    }
  }

  private void OnIsOnlineChanged(bool isOnline)
  {
    if (isOnline && !_proxies.ContainsKey(MultiplayerRepo.LocalPeerId.Value))
    {
      CreateProxyForLocalPlayer();
    }
  }

  public void OnPhysicsProcess(double delta)
  {
    if (!Multiplayer.HasMultiplayerPeer())
    { return; }

    if (Multiplayer.IsServer() || Multiplayer.GetUniqueId() == MultiplayerRepo.LocalPeerId.Value)
    {
      _syncTimer += (float)delta;
      if (_syncTimer >= SYNC_INTERVAL)
      {
        _syncTimer = 0f;
        SyncLocalPlayerPosition();
      }
    }
  }

  private void CreateProxyForLocalPlayer()
  {
    var player = GetPlayer();
    if (player == null)
    { return; }

    var localPeerId = MultiplayerRepo.LocalPeerId.Value;
    if (_proxies.ContainsKey(localPeerId))
    { return; }

    var proxy = new CollectorProxy();
    proxy.Initialize(localPeerId);
    proxy.UpdatePosition(player.GlobalPosition);
    AddChild(proxy);
    _proxies[localPeerId] = proxy;

    if (Multiplayer.IsServer())
    {
      Rpc(MethodName.CreateProxyForPeer, localPeerId, player.GlobalPosition);
    }
  }

  private void SyncLocalPlayerPosition()
  {
    var player = GetPlayer();
    if (player == null)
    { return; }

    var localPeerId = MultiplayerRepo.LocalPeerId.Value;
    if (_proxies.TryGetValue(localPeerId, out var proxy))
    {
      proxy.UpdatePosition(player.GlobalPosition);
      Rpc(MethodName.UpdateProxyPosition, localPeerId, player.GlobalPosition);
    }
  }

  private void OnPeerConnected(int peerId)
  {
    if (peerId == MultiplayerRepo.LocalPeerId.Value)
    {
      CreateProxyForLocalPlayer();
    }
    else if (Multiplayer.IsServer())
    {
      var player = GetPlayer();
      if (player != null)
      {
        RpcId(peerId, MethodName.CreateProxyForPeer, MultiplayerRepo.LocalPeerId.Value, player.GlobalPosition);
      }

      foreach (var existingPeer in MultiplayerRepo.Peers.Values)
      {
        if (existingPeer.PeerId != peerId && existingPeer.PeerId != MultiplayerRepo.LocalPeerId.Value)
        {
          if (_proxies.TryGetValue(existingPeer.PeerId, out var existingProxy))
          {
            RpcId(peerId, MethodName.CreateProxyForPeer, existingPeer.PeerId, existingProxy.GlobalPosition);
          }
        }
      }
    }
  }

  private IPlayer? GetPlayer()
  {
    return EntityTable.Get<IPlayer>("Player");
  }

  private void OnPeerDisconnected(int peerId)
  {
    if (_proxies.TryGetValue(peerId, out var proxy))
    {
      proxy.QueueFree();
      _proxies.Remove(peerId);
    }
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void CreateProxyForPeer(int peerId, Vector3 initialPosition)
  {
    if (_proxies.ContainsKey(peerId))
    { return; }

    var proxy = new CollectorProxy();
    proxy.Initialize(peerId);
    proxy.UpdatePosition(initialPosition);
    AddChild(proxy);
    _proxies[peerId] = proxy;
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
  private void UpdateProxyPosition(int peerId, Vector3 position)
  {
    if (peerId == MultiplayerRepo.LocalPeerId.Value)
    { return; }

    if (_proxies.TryGetValue(peerId, out var proxy))
    {
      proxy.UpdatePosition(position);
    }
  }

  public void OnExitTree()
  {
    MultiplayerRepo.PeerConnected -= OnPeerConnected;
    MultiplayerRepo.PeerDisconnected -= OnPeerDisconnected;
    MultiplayerRepo.IsOnline.Sync -= OnIsOnlineChanged;
  }
}

