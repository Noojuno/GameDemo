namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public interface IFirstPersonCameraLogic : ILogicBlock<FirstPersonCameraLogic.State>;

[Meta, Id("first_person_camera_logic")]
[LogicBlock(typeof(State), Diagram = true)]
public partial class FirstPersonCameraLogic :
  LogicBlock<FirstPersonCameraLogic.State>, IFirstPersonCameraLogic, IPlayerCameraLogic
{
  public override Transition GetInitialState() => To<State.InputDisabled>();

  object IPlayerCameraLogic.Value => Value;
}

