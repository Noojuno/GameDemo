namespace GameDemo;

using Chickensoft.Introspection;

public partial class MultiplayerLogic
{
  [Meta]
  public static partial class Output
  {
    [Meta, Id("multiplayer_logic_output_network_active")]
    public readonly partial record struct NetworkActive();

    [Meta, Id("multiplayer_logic_output_network_inactive")]
    public readonly partial record struct NetworkInactive();

    [Meta, Id("multiplayer_logic_output_hosting_started")]
    public readonly partial record struct HostingStarted(int Port);

    [Meta, Id("multiplayer_logic_output_joined_game")]
    public readonly partial record struct JoinedGame(string Address, int Port);

    [Meta, Id("multiplayer_logic_output_connection_failed")]
    public readonly partial record struct ConnectionFailed();

    [Meta, Id("multiplayer_logic_output_disconnected")]
    public readonly partial record struct Disconnected();
  }
}

