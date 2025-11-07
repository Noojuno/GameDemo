namespace GameDemo;

using Chickensoft.Introspection;

public partial class MultiplayerLogic
{
  [Meta]
  public static partial class Input
  {
    [Meta, Id("multiplayer_logic_input_host_game")]
    public readonly partial record struct HostGame(int Port = 7777);

    [Meta, Id("multiplayer_logic_input_join_game")]
    public readonly partial record struct JoinGame(string Address = "127.0.0.1", int Port = 7777);

    [Meta, Id("multiplayer_logic_input_disconnect")]
    public readonly partial record struct Disconnect();

    [Meta, Id("multiplayer_logic_input_peer_connected")]
    public readonly partial record struct PeerConnected(int PeerId, string PlayerName);

    [Meta, Id("multiplayer_logic_input_peer_disconnected")]
    public readonly partial record struct PeerDisconnected(int PeerId);

    [Meta, Id("multiplayer_logic_input_connected_to_server")]
    public readonly partial record struct ConnectedToServer(int LocalPeerId);

    [Meta, Id("multiplayer_logic_input_connection_failed")]
    public readonly partial record struct ConnectionFailed();

    [Meta, Id("multiplayer_logic_input_server_disconnected")]
    public readonly partial record struct ServerDisconnected();
  }
}

