namespace GameDemo;

using Chickensoft.AutoInject;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Chickensoft.SaveFileBuilder;
using Godot;

/// <summary>
///   First-person camera interface. Simplified camera system for first-person view.
/// </summary>
public interface IFirstPersonCamera : INode3D
{
  IFirstPersonCameraLogic CameraLogic { get; }

  /// <summary>
  ///   Camera system's overall offset that doesn't change during
  ///   runtime. The camera's position is determined by the target offset
  ///   (usually the player's global position) added to this.
  /// </summary>
  Vector3 Offset { get; }

  /// <summary>Camera's local position within the camera system.</summary>
  Vector3 CameraLocalPosition { get; }

  /// <summary>Horizontal gimbal rotation in euler angles.</summary>
  Vector3 GimbalRotationHorizontal { get; }

  /// <summary>Vertical gimbal rotation in euler angles.</summary>
  Vector3 GimbalRotationVertical { get; }

  /// <summary>Camera's global transform basis.</summary>
  Basis CameraBasis { get; }

  /// <summary>Sets the current camera to the first-person camera.</summary>
  void UseFirstPersonCamera();
}

[Meta(typeof(IAutoNode))]
public partial class FirstPersonCamera : Node3D, IFirstPersonCamera, IPlayerCamera
{
  public override void _Notification(int what) => this.Notify(what);

  #region Save

  [Dependency]
  public ISaveChunk<GameData> GameChunk => this.DependOn<ISaveChunk<GameData>>();
  public ISaveChunk<FirstPersonCameraData> FirstPersonCameraChunk { get; set; } = default!;
  #endregion Save

  #region State
  [Dependency] public IGameRepo GameRepo => this.DependOn<IGameRepo>();

  public IFirstPersonCameraLogic CameraLogic { get; set; } = default!;

  public FirstPersonCameraLogic.IBinding CameraBinding { get; set; } = default!;

  #endregion State

  #region Exports

  [Export] public Vector3 Offset { get; set; } = new Vector3(0f, 1.6f, 0f); // Eye height offset

  [Export(PropertyHint.ResourceType, "FirstPersonCameraSettings")]
  public FirstPersonCameraSettings Settings { get; set; } = new();

  #endregion Exports

  #region Nodes

  [Node("%GimbalHorizontal")]
  public INode3D GimbalHorizontalNode { get; set; } = default!;

  [Node("%GimbalVertical")]
  public INode3D GimbalVerticalNode { get; set; } = default!;

  [Node("%Camera3D")] public ICamera3D CameraNode { get; set; } = default!;

  #endregion Nodes

  #region Computed

  public Vector3 CameraLocalPosition => CameraNode.Position;
  public Vector3 GimbalRotationHorizontal => GimbalHorizontalNode.Rotation;
  public Vector3 GimbalRotationVertical => GimbalVerticalNode.Rotation;

  public Basis CameraBasis => GimbalHorizontalNode.GlobalTransform.Basis;

  #endregion Computed

  public void Setup()
  {
    CameraLogic = new FirstPersonCameraLogic();

    CameraLogic.Set(this as IFirstPersonCamera);
    CameraLogic.Set(Settings);
    CameraLogic.Set(GameRepo);

    CameraLogic.Save(
      () => new FirstPersonCameraLogic.Data
      {
        TargetPosition = Vector3.Zero,
        TargetAngleHorizontal = 0f,
        TargetAngleVertical = 0f
      }
    );

    FirstPersonCameraChunk = new SaveChunk<FirstPersonCameraData>(
      onSave: (chunk) => new FirstPersonCameraData()
      {
        StateMachine = CameraLogic,
        GlobalTransform = GlobalTransform,
        LocalPosition = CameraNode.Position,
      },
      onLoad: (chunk, data) =>
      {
        CameraLogic.RestoreFrom(data.StateMachine);
        GlobalTransform = data.GlobalTransform;
        CameraNode.Position = data.LocalPosition;

        CameraLogic.Input(new FirstPersonCameraLogic.Input.PhysicsTicked(0d));
      }
    );

    SetPhysicsProcess(true);
  }

  public void OnResolved()
  {
    GameChunk.AddChunk(FirstPersonCameraChunk);

    CameraBinding = CameraLogic.Bind();
    CameraBinding
      .Handle((in FirstPersonCameraLogic.Output.GimbalRotationChanged output) =>
      {
        GimbalHorizontalNode.Rotation = output.GimbalRotationHorizontal;
        GimbalVerticalNode.Rotation = output.GimbalRotationVertical;
      })
      .Handle((in FirstPersonCameraLogic.Output.GlobalTransformChanged output) =>
        GlobalTransform = output.GlobalTransform
      )
      .Handle(
        (in FirstPersonCameraLogic.Output.CameraLocalPositionChanged output) =>
          CameraNode.Position = output.CameraLocalPosition
      );

    CameraLogic.Start();
  }

  public void OnPhysicsProcess(double delta)
  {
    var xMotion = InputUtilities.GetJoyPadActionPressedMotion(
      "camera_left", "camera_right", JoyAxis.RightX
    );

    if (xMotion is not null)
    {
      CameraLogic.Input(new FirstPersonCameraLogic.Input.JoyPadInputOccurred(xMotion));
    }

    var yMotion = InputUtilities.GetJoyPadActionPressedMotion(
      "camera_up", "camera_down", JoyAxis.RightY
    );

    if (yMotion is not null)
    {
      CameraLogic.Input(new FirstPersonCameraLogic.Input.JoyPadInputOccurred(yMotion));
    }

    CameraLogic.Input(
      new FirstPersonCameraLogic.Input.PhysicsTicked(delta)
    );
  }

  public override void _Input(InputEvent @event)
  {
    if (@event is InputEventMouseMotion motion)
    {
      CameraLogic.Input(new FirstPersonCameraLogic.Input.MouseInputOccurred(motion));
    }
  }

  public void UseFirstPersonCamera() => CameraNode.MakeCurrent();

  // IPlayerCamera compatibility - delegate to UseFirstPersonCamera
  public void UsePlayerCamera() => UseFirstPersonCamera();

  // IPlayerCamera compatibility - not used in first-person but required by interface
  public Vector3 SpringArmTargetPosition => Vector3.Zero;
  public Vector3 OffsetPosition => Vector3.Zero;

  // IPlayerCamera compatibility - return FirstPersonCameraLogic wrapped
  IPlayerCameraLogic IPlayerCamera.CameraLogic => throw new System.NotSupportedException("FirstPersonCamera uses IFirstPersonCameraLogic, not IPlayerCameraLogic");

  public void OnExitTree()
  {
    CameraLogic.Stop();
    CameraBinding.Dispose();
  }
}

