namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public interface IMultiplayerLogic : ILogicBlock<MultiplayerLogic.State>;

[Meta, Id("multiplayer_logic")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class MultiplayerLogic : LogicBlock<MultiplayerLogic.State>, IMultiplayerLogic
{
  public override Transition GetInitialState() => To<State.Offline>();
}

