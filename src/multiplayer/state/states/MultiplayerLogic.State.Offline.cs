namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class MultiplayerLogic
{
  public partial record State
  {
    [Meta, Id("multiplayer_logic_state_offline")]
    public partial record Offline : State,
      IGet<Input.HostGame>,
      IGet<Input.JoinGame>
    {
      public Transition On(in Input.HostGame input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.StartHosting(input.Port);
        Output(new Output.HostingStarted(input.Port));
        return To<Hosting>();
      }

      public Transition On(in Input.JoinGame input)
      {
        var repo = Get<IMultiplayerRepo>();
        repo.JoinGame(input.Address, input.Port);
        Output(new Output.JoinedGame(input.Address, input.Port));
        return To<Client>();
      }
    }
  }
}

