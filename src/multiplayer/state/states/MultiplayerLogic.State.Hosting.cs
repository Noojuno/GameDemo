namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class MultiplayerLogic
{
  public partial record State
  {
    [Meta, Id("multiplayer_logic_state_hosting")]
    public partial record Hosting : State,
      IGet<Input.PeerConnected>,
      IGet<Input.PeerDisconnected>,
      IGet<Input.Disconnect>
    {
      public Transition On(in Input.PeerConnected input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.RegisterPeer(input.PeerId, input.PlayerName);
        return ToSelf();
      }

      public Transition On(in Input.PeerDisconnected input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.UnregisterPeer(input.PeerId);
        return ToSelf();
      }

      public Transition On(in Input.Disconnect input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.Disconnect();
        Output(new Output.Disconnected());
        return To<Offline>();
      }
    }
  }
}

