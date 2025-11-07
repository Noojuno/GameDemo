namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class MultiplayerLogic
{
  [Meta]
  public abstract partial record State : StateLogic<State>
  {
    protected State()
    {
      OnAttach(() =>
      {
        var repo = Get<IMultiplayerRepo>();
        repo.IsOnline.Sync += OnIsOnlineChanged;
      });

      OnDetach(() =>
      {
        var repo = Get<IMultiplayerRepo>();
        repo.IsOnline.Sync -= OnIsOnlineChanged;
      });
    }

    public void OnIsOnlineChanged(bool isOnline)
    {
      if (isOnline)
      {
        Output(new Output.NetworkActive());
      }
      else
      {
        Output(new Output.NetworkInactive());
      }
    }
  }
}

