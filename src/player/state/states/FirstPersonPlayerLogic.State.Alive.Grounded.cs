namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class FirstPersonPlayerLogic
{
  public abstract partial record State
  {
    [Meta]
    public abstract partial record Grounded : Alive,
    IGet<Input.Jump>, IGet<Input.LeftFloor>
    {
      public virtual Transition On(in Input.Jump input)
      {
        // We can jump from any grounded state if the jump button was just
        // pressed.
        var player = Get<IPlayer>();
        var settings = Get<Settings>();

        var velocity = player.Velocity;

        // Start the jump.
        velocity.Y += settings.JumpImpulseForce;
        Output(new Output.VelocityChanged(velocity));

        return To<Jumping>();
      }

      public Transition On(in Input.LeftFloor input)
      {
        if (input.IsFalling)
        {
          return To<Falling>();
        }
        // We got pushed into the air by something that isn't the player's jump
        // input, so we have a separate state for that.
        return To<Liftoff>();
      }
    }

    [Meta, Id("first_person_player_logic_state_alive_grounded_crouching")]
    public abstract partial record Crouching : Grounded,
      IGet<Input.CrouchEnded>,
      IGet<Input.DashRequested>
    {
      protected override float GetSpeedMultiplier(Data data, Settings settings) =>
        settings.CrouchSpeedMultiplier;

      public override Transition On(in Input.CrouchEnded input)
      {
        base.On(input);
        var data = Get<Data>();
        data.CameraOffset = 0f;

        var player = Get<IPlayer>();

        if (player.IsMovingHorizontally())
        {
          if (data.IsSprintHeld)
          {
            return To<Sprinting>();
          }

          return To<Moving>();
        }

        return To<Idle>();
      }

      public override Transition On(in Input.DashRequested input)
      {
        if (TryStartDash(input.Direction))
        {
          return To<Dashing>();
        }

        return ToSelf();
      }

      [Meta, Id("first_person_player_logic_state_alive_grounded_crouching_idle")]
      public partial record IdleState : Crouching,
        IGet<Input.StartedMovingHorizontally>
      {
        public IdleState()
        {
          this.OnEnter(() => Output(new Output.Animations.Idle()));
        }

        public Transition On(in Input.StartedMovingHorizontally input) =>
          To<MovingState>();
      }

      [Meta, Id("first_person_player_logic_state_alive_grounded_crouching_moving")]
      public partial record MovingState : Crouching,
        IGet<Input.StoppedMovingHorizontally>
      {
        public MovingState()
        {
          this.OnEnter(() => Output(new Output.Animations.Move()));
        }

        public Transition On(in Input.StoppedMovingHorizontally input) =>
          To<IdleState>();
      }
    }

    [Meta, Id("first_person_player_logic_state_alive_grounded_dashing")]
    public partial record Dashing : Grounded
    {
      public Dashing()
      {
        this.OnEnter(() =>
        {
          var data = Get<Data>();
          var settings = Get<Settings>();
          var player = Get<IPlayer>();
          var dashDirection = data.DashDirection with { Y = 0f };

          if (dashDirection.LengthSquared() > Mathf.Epsilon)
          {
            dashDirection = dashDirection.Normalized();
            var velocity = player.Velocity;
            velocity.X = dashDirection.X * settings.DashSpeed;
            velocity.Z = dashDirection.Z * settings.DashSpeed;
            Output(new Output.VelocityChanged(velocity));
          }
        });
      }

      public override Transition On(in Input.PhysicsTick input)
      {
        var data = Get<Data>();
        var settings = Get<Settings>();
        var player = Get<IPlayer>();
        var delta = (float)input.Delta;

        UpdateDashCooldown(data, delta);

        if (data.DashTimer > 0f)
        {
          data.DashTimer = Mathf.Max(0f, data.DashTimer - delta);
        }

        var dashDirection = data.DashDirection with { Y = 0f };
        var hasDirection = dashDirection.LengthSquared() > Mathf.Epsilon;

        if (hasDirection)
        {
          dashDirection = dashDirection.Normalized();
        }

        var velocity = player.Velocity;
        velocity.Y += settings.Gravity * delta;

        if (hasDirection)
        {
          velocity.X = dashDirection.X * settings.DashSpeed;
          velocity.Z = dashDirection.Z * settings.DashSpeed;
        }

        var dashVector = hasDirection ? new Vector2(dashDirection.X, dashDirection.Z) : Vector2.Zero;

        Output(new Output.MovementComputed(
          velocity,
          dashVector,
          input.Delta
        ));

        if (data.DashTimer <= 0f)
        {
          return ResolvePostDashState();
        }

        return ToSelf();
      }

      private Transition ResolvePostDashState()
      {
        var data = Get<Data>();
        var player = Get<IPlayer>();

        if (!player.IsOnFloor())
        {
          return player.Velocity.Y > 0f
            ? To<Liftoff>()
            : To<Falling>();
        }

        if (data.IsCrouchHeld)
        {
          return player.IsMovingHorizontally()
            ? To<Crouching.MovingState>()
            : To<Crouching.IdleState>();
        }

        if (player.IsMovingHorizontally())
        {
          if (data.IsSprintHeld)
          {
            return To<Sprinting>();
          }

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
        return ToSelf();
      }

      public override Transition On(in Input.CrouchEnded input)
      {
        base.On(input);
        var data = Get<Data>();
        data.CameraOffset = 0f;
        return ToSelf();
      }

      public override Transition On(in Input.DashRequested input) => ToSelf();
    }
  }
}

