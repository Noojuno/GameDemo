namespace GameDemo;

using System;
using System.Collections.Generic;
using Chickensoft.Collections;

/// <summary>
///   Multiplayer repository — stores multiplayer state and manages network peers.
/// </summary>
public class MultiplayerRepo : IMultiplayerRepo
{
  public IAutoProp<bool> IsHosting => _isHosting;
  private readonly AutoProp<bool> _isHosting;

  public IAutoProp<bool> IsClient => _isClient;
  private readonly AutoProp<bool> _isClient;

  public IAutoProp<bool> IsOnline => _isOnline;
  private readonly AutoProp<bool> _isOnline;

  public IAutoProp<int> LocalPeerId => _localPeerId;
  private readonly AutoProp<int> _localPeerId;

  public IReadOnlyDictionary<int, NetworkPeerInfo> Peers => _peers;
  private readonly Dictionary<int, NetworkPeerInfo> _peers = new();

  public event Action<int>? PeerConnected;
  public event Action<int>? PeerDisconnected;
  public event Action? ServerDisconnected;

  private bool _disposedValue;

  public MultiplayerRepo()
  {
    _isHosting = new AutoProp<bool>(false);
    _isClient = new AutoProp<bool>(false);
    _isOnline = new AutoProp<bool>(false);
    _localPeerId = new AutoProp<int>(1);
  }

  public void StartHosting(int port = 7777)
  {
    _isHosting.OnNext(true);
    _isClient.OnNext(false);
    _isOnline.OnNext(true);
    _localPeerId.OnNext(1);
    
    RegisterPeer(1, "Host");
  }

  public void JoinGame(string address = "127.0.0.1", int port = 7777)
  {
    _isHosting.OnNext(false);
    _isClient.OnNext(true);
  }

  public void OnConnectedToServer(int localPeerId)
  {
    _isOnline.OnNext(true);
    _localPeerId.OnNext(localPeerId);
  }

  public void OnConnectionFailed()
  {
    _isHosting.OnNext(false);
    _isClient.OnNext(false);
    _isOnline.OnNext(false);
  }

  public void OnServerDisconnected()
  {
    ServerDisconnected?.Invoke();
    Disconnect();
  }

  public void Disconnect()
  {
    _peers.Clear();
    _isHosting.OnNext(false);
    _isClient.OnNext(false);
    _isOnline.OnNext(false);
    _localPeerId.OnNext(1);
  }

  public void RegisterPeer(int peerId, string playerName)
  {
    var peerInfo = new NetworkPeerInfo { PeerId = peerId, PlayerName = playerName };
    _peers[peerId] = peerInfo;
    PeerConnected?.Invoke(peerId);
  }

  public void UnregisterPeer(int peerId)
  {
    if (_peers.Remove(peerId))
    {
      PeerDisconnected?.Invoke(peerId);
    }
  }

  protected void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        _isHosting.OnCompleted();
        _isHosting.Dispose();

        _isClient.OnCompleted();
        _isClient.Dispose();

        _isOnline.OnCompleted();
        _isOnline.Dispose();

        _localPeerId.OnCompleted();
        _localPeerId.Dispose();
      }

      _disposedValue = true;
    }
  }

  public void Dispose()
  {
    Dispose(disposing: true);
    GC.SuppressFinalize(this);
  }
}

