namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class FirstPersonPlayerLogic
{
  public abstract partial record State
  {
    [Meta, Id("first_person_player_logic_state_alive_grounded_idle")]
    public partial record Idle : Grounded,
    IGet<Input.StartedMovingHorizontally>,
    IGet<Input.CrouchStarted>,
    IGet<Input.DashRequested>
    {
      public Idle()
      {
        this.OnEnter(() => Output(new Output.Animations.Idle()));
      }

      public Transition On(in Input.StartedMovingHorizontally input) =>
        Get<Data>().IsSprintHeld
          ? To<Sprinting>()
          : To<Moving>();

      public override Transition On(in Input.CrouchStarted input)
      {
        base.On(input);
        var data = Get<Data>();
        var settings = Get<Settings>();
        data.CameraOffset = settings.CrouchCameraOffset;
        return To<Crouching.IdleState>();
      }

      public override Transition On(in Input.DashRequested input)
      {
        if (TryStartDash(input.Direction))
        {
          return To<Dashing>();
        }

        return ToSelf();
      }
    }
  }
}

