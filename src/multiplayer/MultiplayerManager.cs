namespace GameDemo;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

public interface IMultiplayerManager : INode,
  IProvide<IMultiplayerRepo>, IProvide<IMultiplayerLogic>
{
}

[Meta(typeof(IAutoNode))]
public partial class MultiplayerManager : Node, IMultiplayerManager
{
  public override void _Notification(int what) => this.Notify(what);

  #region Provisions

  IMultiplayerRepo IProvide<IMultiplayerRepo>.Value() => MultiplayerRepo;
  IMultiplayerLogic IProvide<IMultiplayerLogic>.Value() => MultiplayerLogic;

  #endregion Provisions

  #region State

  public IMultiplayerRepo MultiplayerRepo { get; set; } = default!;
  public IMultiplayerLogic MultiplayerLogic { get; set; } = default!;
  public MultiplayerLogic.IBinding MultiplayerBinding { get; set; } = default!;

  #endregion State

  public void Setup()
  {
    MultiplayerRepo = new MultiplayerRepo();
    MultiplayerLogic = new MultiplayerLogic();
    MultiplayerLogic.Set(MultiplayerRepo);

    var multiplayer = GetTree().GetMultiplayer();
    multiplayer.PeerConnected += OnPeerConnected;
    multiplayer.PeerDisconnected += OnPeerDisconnected;
    multiplayer.ConnectedToServer += OnConnectedToServer;
    multiplayer.ConnectionFailed += OnConnectionFailed;
    multiplayer.ServerDisconnected += OnServerDisconnected;
  }

  public void OnReady()
  {
    MultiplayerBinding = MultiplayerLogic.Bind();

    MultiplayerBinding
      .Handle((in MultiplayerLogic.Output.HostingStarted output) =>
      {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(output.Port);
        if (error != Error.Ok)
        {
          GD.PrintErr($"Failed to start server: {error}");
          MultiplayerLogic.Input(new MultiplayerLogic.Input.Disconnect());
          return;
        }
        GetTree().GetMultiplayer().MultiplayerPeer = peer;
        GD.Print($"Server started on port {output.Port}");
      })
      .Handle((in MultiplayerLogic.Output.JoinedGame output) =>
      {
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(output.Address, output.Port);
        if (error != Error.Ok)
        {
          GD.PrintErr($"Failed to connect to server: {error}");
          MultiplayerLogic.Input(new MultiplayerLogic.Input.ConnectionFailed());
          return;
        }
        GetTree().GetMultiplayer().MultiplayerPeer = peer;
        GD.Print($"Connecting to {output.Address}:{output.Port}");
      })
      .Handle((in MultiplayerLogic.Output.Disconnected _) =>
      {
        var multiplayer = GetTree().GetMultiplayer();
        if (multiplayer.MultiplayerPeer != null)
        {
          multiplayer.MultiplayerPeer.Close();
          multiplayer.MultiplayerPeer = null;
        }
        GD.Print("Disconnected from network");
      });

    MultiplayerLogic.Start();
    this.Provide();
  }

  public void HostGame(int port = 7777)
  {
    MultiplayerLogic.Input(new MultiplayerLogic.Input.HostGame(port));
  }

  public void JoinGame(string address = "127.0.0.1", int port = 7777)
  {
    MultiplayerLogic.Input(new MultiplayerLogic.Input.JoinGame(address, port));
  }

  public void Disconnect()
  {
    MultiplayerLogic.Input(new MultiplayerLogic.Input.Disconnect());
  }

  private void OnPeerConnected(long peerId)
  {
    GD.Print($"Peer {peerId} connected");
    
    if (Multiplayer.IsServer())
    {
      RpcId(peerId, MethodName.RegisterPeerOnClient, peerId, $"Player{peerId}");
    }
    
    MultiplayerLogic.Input(
      new MultiplayerLogic.Input.PeerConnected((int)peerId, $"Player{peerId}")
    );
  }

  private void OnPeerDisconnected(long peerId)
  {
    GD.Print($"Peer {peerId} disconnected");
    MultiplayerLogic.Input(new MultiplayerLogic.Input.PeerDisconnected((int)peerId));
  }

  private void OnConnectedToServer()
  {
    var localPeerId = Multiplayer.GetUniqueId();
    GD.Print($"Connected to server as peer {localPeerId}");
    MultiplayerLogic.Input(
      new MultiplayerLogic.Input.ConnectedToServer(localPeerId)
    );
    
    RpcId(1, MethodName.RegisterPeerOnServer, localPeerId, $"Player{localPeerId}");
  }

  private void OnConnectionFailed()
  {
    GD.PrintErr("Connection to server failed");
    MultiplayerLogic.Input(new MultiplayerLogic.Input.ConnectionFailed());
  }

  private void OnServerDisconnected()
  {
    GD.PrintErr("Server disconnected");
    MultiplayerLogic.Input(new MultiplayerLogic.Input.ServerDisconnected());
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RegisterPeerOnServer(int peerId, string playerName)
  {
    if (!Multiplayer.IsServer()) { return; }
    
    MultiplayerLogic.Input(
      new MultiplayerLogic.Input.PeerConnected(peerId, playerName)
    );
    
    foreach (var existingPeer in MultiplayerRepo.Peers.Values)
    {
      RpcId(peerId, MethodName.RegisterPeerOnClient, existingPeer.PeerId, existingPeer.PlayerName);
    }
    
    Rpc(MethodName.RegisterPeerOnClient, peerId, playerName);
  }

  [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
  private void RegisterPeerOnClient(int peerId, string playerName)
  {
    MultiplayerLogic.Input(
      new MultiplayerLogic.Input.PeerConnected(peerId, playerName)
    );
  }

  public void OnExitTree()
  {
    var multiplayer = GetTree().GetMultiplayer();
    multiplayer.PeerConnected -= OnPeerConnected;
    multiplayer.PeerDisconnected -= OnPeerDisconnected;
    multiplayer.ConnectedToServer -= OnConnectedToServer;
    multiplayer.ConnectionFailed -= OnConnectionFailed;
    multiplayer.ServerDisconnected -= OnServerDisconnected;

    MultiplayerLogic.Stop();
    MultiplayerBinding.Dispose();
    MultiplayerRepo.Dispose();
  }
}

