namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class MultiplayerLogic
{
  public partial record State
  {
    [Meta, Id("multiplayer_logic_state_client")]
    public partial record Client : State,
      IGet<Input.ConnectedToServer>,
      IGet<Input.ConnectionFailed>,
      IGet<Input.ServerDisconnected>,
      IGet<Input.Disconnect>
    {
      public Transition On(in Input.ConnectedToServer input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.OnConnectedToServer(input.LocalPeerId);
        return ToSelf();
      }

      public Transition On(in Input.ConnectionFailed input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.OnConnectionFailed();
        Output(new Output.ConnectionFailed());
        return To<Offline>();
      }

      public Transition On(in Input.ServerDisconnected input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.OnServerDisconnected();
        Output(new Output.Disconnected());
        return To<Offline>();
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

