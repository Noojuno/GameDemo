namespace GameDemo;

using Chickensoft.Introspection;

[Meta, Id("network_peer_info")]
public partial record NetworkPeerInfo
{
  public int PeerId { get; init; }
  public string PlayerName { get; init; } = "";
}

