namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public interface IFirstPersonPlayerLogic : ILogicBlock<FirstPersonPlayerLogic.State>;

[Meta, Id("first_person_player_logic")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class FirstPersonPlayerLogic : LogicBlock<FirstPersonPlayerLogic.State>, IFirstPersonPlayerLogic, IPlayerLogic
{
  public override Transition GetInitialState() => To<State.Disabled>();
  
  object IPlayerLogic.Value => Value;
}

