namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class FirstPersonPlayerLogic
{
  public abstract partial record State
  {
    [Meta, Id("first_person_player_logic_state_alive_grounded_moving")]
    public partial record Moving : Grounded,
    IGet<Input.StoppedMovingHorizontally>,
    IGet<Input.SprintStarted>,
    IGet<Input.CrouchStarted>,
    IGet<Input.DashRequested>
    {
      public Moving()
      {
        this.OnEnter(() => Output(new Output.Animations.Move()));
      }

      public Transition On(in Input.StoppedMovingHorizontally input) =>
        To<Idle>();

      public override Transition On(in Input.SprintStarted input)
      {
        base.On(input);
        return To<Sprinting>();
      }

      public override Transition On(in Input.CrouchStarted input)
      {
        base.On(input);
        var data = Get<Data>();
        var settings = Get<Settings>();
        data.CameraOffset = settings.CrouchCameraOffset;
        return To<Crouching.MovingState>();
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

    [Meta, Id("first_person_player_logic_state_alive_grounded_moving_sprinting")]
    public partial record Sprinting : Moving,
      IGet<Input.SprintEnded>
    {
      protected override float GetSpeedMultiplier(Data data, Settings settings) =>
        settings.SprintSpeedMultiplier;

      public override Transition On(in Input.SprintEnded input)
      {
        base.On(input);

        if (Get<IPlayer>().IsMovingHorizontally())
        {
          return To<Moving>();
        }

        return To<Idle>();
      }

      public override Transition On(in Input.CrouchStarted input)
      {
        base.On(input);
        var data = Get<Data>();
        var settings = Get<Settings>();
        data.CameraOffset = settings.CrouchCameraOffset;
        return To<Crouching.MovingState>();
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

