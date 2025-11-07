namespace GameDemo;

using Chickensoft.Introspection;
using Godot;

public partial class FirstPersonPlayerLogic
{
  public partial record State
  {
    private const float MOVEMENT = 0.2f;

    [Meta]
    public abstract partial record Alive : State,
      IGet<Input.PhysicsTick>,
      IGet<Input.Moved>,
      IGet<Input.Pushed>,
      IGet<Input.Killed>,
      IGet<Input.SprintStarted>,
      IGet<Input.SprintEnded>,
      IGet<Input.CrouchStarted>,
      IGet<Input.CrouchEnded>,
      IGet<Input.DashRequested>,
      IGet<Input.CrouchEdgeBlockedChanged>
    {
      protected virtual float GetSpeedMultiplier(Data data, Settings settings) => 1f;

      protected static void UpdateDashCooldown(Data data, float delta)
      {
        if (data.DashCooldownTimer > 0f)
        {
          data.DashCooldownTimer = Mathf.Max(0f, data.DashCooldownTimer - delta);
        }
      }

      protected bool TryStartDash(Vector3 rawDirection)
      {
        var data = Get<Data>();
        var settings = Get<Settings>();

        if (data.DashTimer > 0f || data.DashCooldownTimer > 0f)
        {
          return false;
        }

        var direction = rawDirection with { Y = 0f };

        if (direction.LengthSquared() <= Mathf.Epsilon)
        {
          direction = data.LastStrongDirection with { Y = 0f };
        }

        if (direction.LengthSquared() <= Mathf.Epsilon)
        {
          return false;
        }

        direction = direction.Normalized();
        data.DashDirection = direction;
        data.LastStrongDirection = direction;
        data.DashTimer = settings.DashDuration;
        data.DashCooldownTimer = settings.DashCooldown;
        return true;
      }

      public virtual Transition On(in Input.Killed input)
      {
        Get<IGameRepo>().OnGameEnded(GameOverReason.Lost);

        return To<Dead>();
      }

      public virtual Transition On(in Input.PhysicsTick input)
      {
        var delta = input.Delta;
        var player = Get<IPlayer>();
        var settings = Get<Settings>();
        var gameRepo = Get<IGameRepo>();
        var data = Get<Data>();

        var cameraBasis = gameRepo.CameraBasis.Value;
        var moveDirection = player.GetGlobalInputVector(cameraBasis);

        var direction = new Vector2(moveDirection.X, moveDirection.Z);

        if (moveDirection.Length() > MOVEMENT)
        {
          data.LastStrongDirection = moveDirection.Normalized();
        }
        else
        {
          direction = new Vector2(data.LastStrongDirection.X, data.LastStrongDirection.Z);
        }

        UpdateDashCooldown(data, (float)delta);

        data.LastVelocity = player.Velocity;
        var velocity = data.LastVelocity with { Y = 0f };

        var speedMultiplier = GetSpeedMultiplier(data, settings);
        var targetVelocity = moveDirection * settings.MoveSpeed * speedMultiplier;

        velocity = velocity.Lerp(
          targetVelocity,
          settings.Acceleration * (float)delta
        );

        if (
          moveDirection.Length() == 0f &&
          velocity.Length() < settings.StoppingSpeed
        )
        {
          velocity = Vector3.Zero;
        }

        velocity.Y = data.LastVelocity.Y;

        // Add gravity.
        velocity.Y += settings.Gravity * (float)delta;

        Output(
          new Output.MovementComputed(
            velocity, direction, delta
          )
        );

        return ToSelf();
      }

      public virtual Transition On(in Input.Moved input)
      {
        var player = Get<IPlayer>();
        var gameRepo = Get<IGameRepo>();
        var data = Get<Data>();
        var settings = Get<Settings>();

        // Tell the game the player has moved.
        // Anything that subscribes to our position (like the camera) will
        // be updated.
        gameRepo.SetPlayerGlobalPosition(
          input.GlobalPosition + new Vector3(0f, data.CameraOffset, 0f)
        );

        var isMovingHorizontally = player.IsMovingHorizontally() || data.IsCrouchEdgeBlocked;
        var isOnFloor = player.IsOnFloor();
        var hasNegativeYVelocity = player.Velocity.Y < 0f;

        var justHitFloor = isOnFloor && !data.WasOnFloor;
        var justLeftFloor = !isOnFloor && data.WasOnFloor;
        var justStartedFalling = hasNegativeYVelocity && !data.HadNegativeYVelocity();

        var wasMovingHorizontally = data.WasMovingHorizontally(settings);

        var justStartedMovingHorizontally =
          isMovingHorizontally && !wasMovingHorizontally;
        var justStoppedMovingHorizontally =
          !isMovingHorizontally && wasMovingHorizontally;

        // Update the cached values so we can use them next frame.
        data.WasOnFloor = isOnFloor;
        data.LastVelocity = player.Velocity;
        data.LastCrouchEdgeBlocked = data.IsCrouchEdgeBlocked;

        if (justHitFloor)
        {
          Input(
            new Input.HitFloor(IsMovingHorizontally: isMovingHorizontally)
          );
        }
        else if (justLeftFloor)
        {
          Input(
            new Input.LeftFloor(IsFalling: hasNegativeYVelocity)
          );
        }
        else if (justStartedFalling)
        {
          Input(new Input.StartedFalling());
        }

        if (justStartedMovingHorizontally)
        {
          Input(new Input.StartedMovingHorizontally());
        }
        else if (justStoppedMovingHorizontally)
        {
          Input(new Input.StoppedMovingHorizontally());
        }

        return ToSelf();
      }

      public Transition On(in Input.Pushed input)
      {
        var player = Get<IPlayer>();
        var velocity = player.Velocity;

        // Apply force
        velocity += input.GlobalForceImpulseVector;
        Output(new Output.VelocityChanged(velocity));

        return ToSelf();
      }

      public virtual Transition On(in Input.SprintStarted input)
      {
        var data = Get<Data>();
        data.IsSprintHeld = true;
        return ToSelf();
      }

      public virtual Transition On(in Input.SprintEnded input)
      {
        var data = Get<Data>();
        data.IsSprintHeld = false;
        return ToSelf();
      }

      public virtual Transition On(in Input.CrouchStarted input)
      {
        var data = Get<Data>();
        data.IsCrouchHeld = true;
        return ToSelf();
      }

      public virtual Transition On(in Input.CrouchEnded input)
      {
        var data = Get<Data>();
        data.IsCrouchHeld = false;
        return ToSelf();
      }

      public virtual Transition On(in Input.DashRequested input) => ToSelf();

      public virtual Transition On(in Input.CrouchEdgeBlockedChanged input)
      {
        var data = Get<Data>();
        data.IsCrouchEdgeBlocked = input.IsBlocked;
        return ToSelf();
      }
    }
  }
}

