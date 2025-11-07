namespace GameDemo;

using System;
using System.Collections.Generic;
using Chickensoft.Collections;

public interface IMultiplayerRepo : IDisposable
{
  /// <summary>Event invoked when a peer connects.</summary>
  event Action<int>? PeerConnected;

  /// <summary>Event invoked when a peer disconnects.</summary>
  event Action<int>? PeerDisconnected;

  /// <summary>Event invoked when the server disconnects.</summary>
  event Action? ServerDisconnected;

  /// <summary>Whether we are currently hosting.</summary>
  IAutoProp<bool> IsHosting { get; }

  /// <summary>Whether we are currently connected as a client.</summary>
  IAutoProp<bool> IsClient { get; }

  /// <summary>Whether we are online (hosting or client).</summary>
  IAutoProp<bool> IsOnline { get; }

  /// <summary>Our local peer ID.</summary>
  IAutoProp<int> LocalPeerId { get; }

  /// <summary>Connected peers.</summary>
  IReadOnlyDictionary<int, NetworkPeerInfo> Peers { get; }

  /// <summary>Start hosting a game.</summary>
  /// <param name="port">Port to host on.</param>
  void StartHosting(int port = 7777);

  /// <summary>Connect to a host.</summary>
  /// <param name="address">Host address.</param>
  /// <param name="port">Host port.</param>
  void JoinGame(string address = "127.0.0.1", int port = 7777);

  /// <summary>Disconnect from the network.</summary>
  void Disconnect();

  /// <summary>Register a peer.</summary>
  /// <param name="peerId">Peer ID.</param>
  /// <param name="playerName">Player name.</param>
  void RegisterPeer(int peerId, string playerName);

  /// <summary>Unregister a peer.</summary>
  /// <param name="peerId">Peer ID.</param>
  void UnregisterPeer(int peerId);

  /// <summary>Called when we successfully connect as a client.</summary>
  void OnConnectedToServer(int localPeerId);

  /// <summary>Called when we fail to connect as a client.</summary>
  void OnConnectionFailed();

  /// <summary>Called when the server disconnects.</summary>
  void OnServerDisconnected();
}

