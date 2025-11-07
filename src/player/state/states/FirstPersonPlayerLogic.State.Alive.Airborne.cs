namespace GameDemo;

using Chickensoft.Introspection;

public partial class FirstPersonPlayerLogic
{
  public partial record State
  {
    [Meta]
    public abstract partial record Airborne : Alive,
      IGet<Input.HitFloor>, IGet<Input.StartedFalling>, IGet<Input.DashRequested>
    {
      public Transition On(in Input.HitFloor input) =>
        input.IsMovingHorizontally ? To<Moving>() : To<Idle>();

      public Transition On(in Input.StartedFalling input) => To<Falling>();

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

