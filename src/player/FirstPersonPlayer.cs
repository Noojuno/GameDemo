namespace GameDemo;

using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Chickensoft.SaveFileBuilder;
using Godot;
using Compiler = System.Runtime.CompilerServices;

[Meta(typeof(IAutoNode))]
public partial class FirstPersonPlayer :
CharacterBody3D,
IPlayer,
IProvide<IPlayerLogic>,
IProvide<FirstPersonPlayerLogic.Settings>
{
  public override void _Notification(int what) => this.Notify(what);

  #region Save

  [Dependency]
  public EntityTable EntityTable => this.DependOn<EntityTable>();
  [Dependency]
  public ISaveChunk<GameData> GameChunk => this.DependOn<ISaveChunk<GameData>>();
  public ISaveChunk<PlayerData> PlayerChunk { get; set; } = default!;
  #endregion Save

  #region Provisions

  IPlayerLogic IProvide<IPlayerLogic>.Value() => PlayerLogic;
  FirstPersonPlayerLogic.Settings IProvide<FirstPersonPlayerLogic.Settings>.Value() => Settings;
  #endregion Provisions

  #region Dependencies

  [Dependency]
  public IGameRepo GameRepo => this.DependOn<IGameRepo>();

  [Dependency]
  public IAppRepo AppRepo => this.DependOn<IAppRepo>();

  [Dependency]
  public IMultiplayerRepo MultiplayerRepo => this.DependOn<IMultiplayerRepo>();

  #endregion Dependencies

  #region Exports

  /// <summary>Stopping velocity (meters/sec).</summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float StoppingSpeed { get; set; } = 1.0f;

  /// <summary>Player gravity (meters/sec).</summary>
  [Export(PropertyHint.Range, "-100, 0, 0.1")]
  public float Gravity { get; set; } = -30.0f;

  /// <summary>Player speed (meters/sec).</summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float MoveSpeed { get; set; } = 8f;

  /// <summary>Player acceleration (meters^2/sec).</summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float Acceleration { get; set; } = 4f;

  /// <summary>Jump initial impulse force.</summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float JumpImpulseForce { get; set; } = 12f;

  /// <summary>
  ///   Additional force added each physics tick while player is still pressing
  ///   jump.
  /// </summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float JumpForce { get; set; } = 4.5f;

  /// <summary>Multiplier applied to move speed while sprinting.</summary>
  [Export(PropertyHint.Range, "1, 5, 0.05")]
  public float SprintSpeedMultiplier { get; set; } = 1.5f;

  /// <summary>Multiplier applied to move speed while crouching.</summary>
  [Export(PropertyHint.Range, "0, 1, 0.05")]
  public float CrouchSpeedMultiplier { get; set; } = 0.5f;

  /// <summary>Scale applied to capsule height while crouching.</summary>
  [Export(PropertyHint.Range, "0.1, 1, 0.05")]
  public float CrouchHeightScale { get; set; } = 0.5f;

  /// <summary>Vertical camera offset applied while crouching.</summary>
  [Export(PropertyHint.Range, "-2, 0, 0.05")]
  public float CrouchCameraOffset { get; set; } = -0.6f;

  /// <summary>Horizontal distance to probe for ledge support while crouched.</summary>
  [Export(PropertyHint.Range, "0, 2, 0.05")]
  public float CrouchLedgeProbeDistance { get; set; } = 0.45f;

  /// <summary>Vertical tolerance when checking for ledge support while crouched.</summary>
  [Export(PropertyHint.Range, "0, 2, 0.05")]
  public float CrouchLedgeVerticalTolerance { get; set; } = 0.75f;

  /// <summary>Horizontal dash speed.</summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float DashSpeed { get; set; } = 20f;

  /// <summary>Dash duration in seconds.</summary>
  [Export(PropertyHint.Range, "0, 5, 0.05")]
  public float DashDuration { get; set; } = 0.25f;

  /// <summary>Dash cooldown in seconds.</summary>
  [Export(PropertyHint.Range, "0, 5, 0.05")]
  public float DashCooldown { get; set; } = 0.6f;

  #endregion Exports

  #region Nodes

  [Node("%PlayerModel")]
  public INode3D PlayerModelNode { get; set; } = default!;

  [Node("%GroundCollisionShape3D")]
  public CollisionShape3D GroundCollisionShape { get; set; } = default!;

  #endregion Nodes

  private CapsuleShape3D? _capsuleShape;
  private float _defaultCapsuleHeight;
  private Vector3 _defaultCollisionShapePosition;
  private bool _isSprinting;
  private bool _isCrouching;
  private float _currentCameraOffset;
  private bool _isCrouchEdgeBlocked;

  private Vector3 _lastSyncedPosition;
  private Vector3 _lastSyncedVelocity;
  private float _syncTimer = 0f;
  private const float SYNC_INTERVAL = 0.05f;
  private bool _isNetworkAuthority = true;

  #region State

  public IPlayerLogic PlayerLogic => FirstPersonPlayerLogic;
  public FirstPersonPlayerLogic FirstPersonPlayerLogic { get; set; } = default!;
  public FirstPersonPlayerLogic.Settings Settings { get; set; } = default!;

  public FirstPersonPlayerLogic.IBinding PlayerBinding { get; set; } = default!;

  #endregion State

  public void Setup()
  {
    Settings = new FirstPersonPlayerLogic.Settings(
      StoppingSpeed,
      Gravity,
      MoveSpeed,
      Acceleration,
      JumpImpulseForce,
      JumpForce,
      SprintSpeedMultiplier,
      CrouchSpeedMultiplier,
      DashSpeed,
      DashDuration,
      DashCooldown,
      CrouchCameraOffset,
      CrouchLedgeProbeDistance,
      CrouchLedgeVerticalTolerance
    );

    FirstPersonPlayerLogic = new FirstPersonPlayerLogic();

    FirstPersonPlayerLogic.Set(this as IPlayer);
    FirstPersonPlayerLogic.Set(Settings);
    FirstPersonPlayerLogic.Set(AppRepo);
    FirstPersonPlayerLogic.Set(GameRepo);
    FirstPersonPlayerLogic.Save(() => new FirstPersonPlayerLogic.Data());

    PlayerChunk = new SaveChunk<PlayerData>(
      onSave: (chunk) => new PlayerData()
      {
        GlobalTransform = GlobalTransform,
        StateMachine = FirstPersonPlayerLogic,
        Velocity = Velocity
      },
      onLoad: (chunk, data) =>
      {
        GlobalTransform = data.GlobalTransform;
        Velocity = data.Velocity;
        if (data.StateMachine is IFirstPersonPlayerLogic fpLogic)
        {
          FirstPersonPlayerLogic.RestoreFrom(fpLogic);
        }
        FirstPersonPlayerLogic.Start();
      }
    );
  }

  public void OnReady()
  {
    RefreshNetworkAuthority();

    if (GroundCollisionShape.Shape is CapsuleShape3D capsule)
    {
      _capsuleShape = (CapsuleShape3D)capsule.Duplicate();
      GroundCollisionShape.Shape = _capsuleShape;
      _defaultCapsuleHeight = _capsuleShape.Height;
      _defaultCollisionShapePosition = GroundCollisionShape.Position;
    }

    _currentCameraOffset = 0f;
  }

  public void OnExitTree()
  {
    EntityTable.Remove(Name);
    FirstPersonPlayerLogic.Stop();
    PlayerBinding.Dispose();
  }

  public void OnResolved()
  {
    // Add a child to our parent save chunk (the game chunk) so that it can
    // look up the player chunk when loading and saving the game.
    GameChunk.AddChunk(PlayerChunk);

    EntityTable.Set(Name, this);

    PlayerBinding = FirstPersonPlayerLogic.Bind();

    GameRepo.SetPlayerGlobalPosition(
      GlobalPosition + new Vector3(0f, _currentCameraOffset, 0f)
    );

    PlayerBinding
      .Handle((in FirstPersonPlayerLogic.Output.MovementComputed output) =>
        Velocity = ApplyCrouchEdgeGuard(in output))
      .Handle((in FirstPersonPlayerLogic.Output.VelocityChanged output) =>
      {
        SetCrouchEdgeBlocked(false);
        Velocity = output.Velocity;
      });

    // Allow the player model to lookup our state machine and bind to it.
    this.Provide();

    // Start the player state machine last.
    FirstPersonPlayerLogic.Start();
  }

  public void OnPhysicsProcess(double delta)
  {
    if (!_isNetworkAuthority)
    {
      return;
    }

    HandleSprint(Input.IsActionPressed(GameInputs.Sprint));
    HandleCrouch(Input.IsActionPressed(GameInputs.Crouch));

    if (Input.IsActionJustPressed(GameInputs.Dash))
    {
      FirstPersonPlayerLogic.Input(
        new FirstPersonPlayerLogic.Input.DashRequested(GetDashDirection())
      );
    }

    FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.PhysicsTick(delta));

    var jumpPressed = Input.IsActionPressed(GameInputs.Jump);
    var jumpJustPressed = Input.IsActionJustPressed(GameInputs.Jump);

    if (ShouldJump(jumpPressed, jumpJustPressed))
    {
      FirstPersonPlayerLogic.Input(
        new FirstPersonPlayerLogic.Input.Jump(delta)
      );
    }

    MoveAndSlide();

    FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.Moved(GlobalPosition));

    SyncNetworkState(delta);
  }

  private void SyncNetworkState(double delta)
  {
    if (!Multiplayer.HasMultiplayerPeer() || !_isNetworkAuthority)
    { return; }

    _syncTimer += (float)delta;
    if (_syncTimer >= SYNC_INTERVAL)
    {
      _syncTimer = 0f;
      var position = GlobalPosition;
      var velocity = Velocity;

      if (position != _lastSyncedPosition || velocity != _lastSyncedVelocity)
      {
        _lastSyncedPosition = position;
        _lastSyncedVelocity = velocity;
        Rpc(MethodName.ReceiveNetworkState, position, velocity);
      }
    }
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
  private void ReceiveNetworkState(Vector3 position, Vector3 velocity)
  {
    if (_isNetworkAuthority)
    { return; }

    GlobalPosition = position;
    Velocity = velocity;
  }

  public static bool ShouldJump(bool jumpPressed, bool jumpJustPressed) =>
    jumpPressed || jumpJustPressed;

  private void HandleSprint(bool isPressed)
  {
    if (isPressed && !_isSprinting && !_isCrouching)
    {
      _isSprinting = true;
      FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.SprintStarted());
      return;
    }

    if ((!isPressed || _isCrouching) && _isSprinting)
    {
      _isSprinting = false;
      FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.SprintEnded());
    }
  }

  private void HandleCrouch(bool isPressed)
  {
    if (isPressed && !_isCrouching)
    {
      _isCrouching = true;
      SetCrouchEdgeBlocked(false);

      if (_isSprinting)
      {
        _isSprinting = false;
        FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.SprintEnded());
      }

      ApplyCrouchCollider(true);
      FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.CrouchStarted());
      return;
    }

    if (!isPressed && _isCrouching)
    {
      _isCrouching = false;
      ApplyCrouchCollider(false);
      SetCrouchEdgeBlocked(false);
      FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.CrouchEnded());
    }
  }

  private void ApplyCrouchCollider(bool isActive)
  {
    if (_capsuleShape is null)
    {
      return;
    }

    if (isActive)
    {
      var scale = Mathf.Clamp(CrouchHeightScale, 0.1f, 1f);
      var adjustedHeight = _defaultCapsuleHeight * scale;
      _capsuleShape.Height = adjustedHeight;
      var heightDifference = _defaultCapsuleHeight - adjustedHeight;
      GroundCollisionShape.Position = _defaultCollisionShapePosition -
        new Vector3(0f, heightDifference / 2f, 0f);
      _currentCameraOffset = CrouchCameraOffset;
    }
    else
    {
      _capsuleShape.Height = _defaultCapsuleHeight;
      GroundCollisionShape.Position = _defaultCollisionShapePosition;
      _currentCameraOffset = 0f;
    }

    GameRepo.SetPlayerGlobalPosition(
      GlobalPosition + new Vector3(0f, _currentCameraOffset, 0f)
    );
  }

  private void SetCrouchEdgeBlocked(bool isBlocked)
  {
    if (_isCrouchEdgeBlocked == isBlocked)
    {
      return;
    }

    _isCrouchEdgeBlocked = isBlocked;
    FirstPersonPlayerLogic.Input(
      new FirstPersonPlayerLogic.Input.CrouchEdgeBlockedChanged(isBlocked)
    );
  }

  private Vector3 ApplyCrouchEdgeGuard(in FirstPersonPlayerLogic.Output.MovementComputed output)
  {
    var desiredVelocity = output.Velocity;

    if (!_isCrouching || !IsOnFloor())
    {
      SetCrouchEdgeBlocked(false);
      return desiredVelocity;
    }

    var horizontalVelocity = desiredVelocity with { Y = 0f };

    if (horizontalVelocity.LengthSquared() <= Mathf.Epsilon)
    {
      SetCrouchEdgeBlocked(false);
      return desiredVelocity;
    }

    var delta = (float)output.Delta;

    if (delta <= Mathf.Epsilon)
    {
      SetCrouchEdgeBlocked(false);
      return desiredVelocity;
    }

    var horizontalDisplacement = horizontalVelocity * delta;

    if (horizontalDisplacement.LengthSquared() <= Mathf.Epsilon)
    {
      SetCrouchEdgeBlocked(false);
      return desiredVelocity;
    }

    var spaceState = GetWorld3D()?.DirectSpaceState;

    if (spaceState is null)
    {
      SetCrouchEdgeBlocked(false);
      return desiredVelocity;
    }

    var direction = horizontalDisplacement.Normalized();
    var probeDistance = Mathf.Max(0f, Settings.CrouchLedgeProbeDistance);
    var verticalTolerance = Mathf.Max(0f, Settings.CrouchLedgeVerticalTolerance);
    var probeOffset = direction * Mathf.Min(probeDistance, horizontalDisplacement.Length());
    var start = GlobalPosition + probeOffset + (Vector3.Up * 0.1f);
    var end = start + (Vector3.Down * (verticalTolerance + 0.1f));

    var query = PhysicsRayQueryParameters3D.Create(start, end);
    query.CollideWithAreas = false;
    query.CollideWithBodies = true;
    query.CollisionMask = CollisionMask;
    query.Exclude.Clear();
    query.Exclude.Add(GetRid());

    var result = spaceState.IntersectRay(query);

    if (result.Count > 0)
    {
      SetCrouchEdgeBlocked(false);
      return desiredVelocity;
    }

    var blockedComponent = direction * horizontalVelocity.Dot(direction);
    horizontalVelocity -= blockedComponent;
    desiredVelocity.X = horizontalVelocity.X;
    desiredVelocity.Z = horizontalVelocity.Z;

    var isBlocked = blockedComponent.LengthSquared() > Mathf.Epsilon;
    SetCrouchEdgeBlocked(isBlocked);

    return desiredVelocity;
  }

  private Vector3 GetDashDirection() =>
    GetGlobalInputVector(GameRepo.CameraBasis.Value);

  #region IPlayer

  public Vector3 GetGlobalInputVector(Basis cameraBasis)
  {
    var rawInput = Input.GetVector(
      GameInputs.MoveLeft, GameInputs.MoveRight, GameInputs.MoveUp,
      GameInputs.MoveDown
    );
    // This is to ensure that diagonal input isn't stronger than axis aligned
    // input.
    var input = new Vector3
    {
      X = rawInput.X * Mathf.Sqrt(1.0f - (rawInput.Y * rawInput.Y / 2.0f)),
      Z = rawInput.Y * Mathf.Sqrt(1.0f - (rawInput.X * rawInput.X / 2.0f))
    };
    // For first-person, movement is relative to camera forward/right
    return cameraBasis * input with { Y = 0f };
  }

  public Basis GetNextRotationBasis(
    Vector3 direction,
    double delta,
    float rotationSpeed
  )
  {
    // First-person doesn't rotate the character body - camera handles rotation
    // Return identity basis scaled properly
    var scale = Transform.Basis.Scale;
    return new Basis().Scaled(scale);
  }

  [Compiler.MethodImpl(Compiler.MethodImplOptions.AggressiveInlining)]
  public bool IsMovingHorizontally() =>
    _isCrouchEdgeBlocked ||
    (Velocity with { Y = 0f }).Length() > Settings.StoppingSpeed;

  #endregion IPlayer

  public void RefreshNetworkAuthority()
  {
    _isNetworkAuthority = !Multiplayer.HasMultiplayerPeer() || IsMultiplayerAuthority();
    SetPhysicsProcess(_isNetworkAuthority);

    if (PlayerModelNode is not null)
    {
      PlayerModelNode.Visible = !_isNetworkAuthority;
    }
  }

  #region IPushEnabled

  public void Push(Vector3 force) =>
    FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.Pushed(force));

  #endregion IPushEnabled

  #region ICoinCollector

  public Vector3 CenterOfMass => GlobalPosition + new Vector3(0f, 1f, 0f);

  #endregion ICoinCollector

  #region IKillable

  public void Kill() => FirstPersonPlayerLogic.Input(new FirstPersonPlayerLogic.Input.Killed());

  #endregion IKillable
}

