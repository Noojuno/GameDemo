namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;
using Godot;

public partial class FirstPersonCameraLogic
{
  /// <summary>
  ///   Overall first-person camera state.
  /// </summary>
  [Meta]
  public abstract partial record State : StateLogic<State>,
    IGet<Input.PhysicsTicked>,
    IGet<Input.TargetPositionChanged>
  {
    public State()
    {
      OnAttach(
        () =>
        {
          var gameRepo = Get<IGameRepo>();
          gameRepo.IsMouseCaptured.Sync += OnMouseCaptured;
          gameRepo.PlayerGlobalPosition.Sync += OnPlayerGlobalPositionChanged;
        }
      );

      OnDetach(
        () =>
        {
          var gameRepo = Get<IGameRepo>();
          gameRepo.IsMouseCaptured.Sync -= OnMouseCaptured;
          gameRepo.PlayerGlobalPosition.Sync -= OnPlayerGlobalPositionChanged;
        }
      );
    }

    internal void OnMouseCaptured(bool isMouseCaptured)
    {
      if (isMouseCaptured)
      {
        Input(new Input.EnableInput());
        return;
      }

      Input(new Input.DisableInput());
    }

    internal void OnPlayerGlobalPositionChanged(Vector3 position) =>
      Input(new Input.TargetPositionChanged(position));

    public Transition On(in Input.PhysicsTicked input)
    {
      var camera = Get<IFirstPersonCamera>();
      var gameRepo = Get<IGameRepo>();
      var settings = Get<FirstPersonCameraSettings>();
      var data = Get<Data>();

      // Lerp to the desired horizontal angle.
      var rotationHorizontal = camera.GimbalRotationHorizontal;
      var rotationHorizontalY = Mathf.RadToDeg(rotationHorizontal.Y);
      rotationHorizontal.Y = Mathf.DegToRad(Mathf.Lerp(
        rotationHorizontalY,
        data.TargetAngleHorizontal,
        (float)input.Delta * settings.HorizontalRotationAcceleration
      ));

      // Lerp to the desired vertical angle.
      var rotationVertical = camera.GimbalRotationVertical;
      var rotationVerticalX = Mathf.RadToDeg(rotationVertical.X);
      rotationVertical.X = Mathf.DegToRad(Mathf.Lerp(
        rotationVerticalX,
        data.TargetAngleVertical,
        (float)input.Delta * settings.VerticalRotationAcceleration
      ));

      // Update gimbal rotations
      Output(new Output.GimbalRotationChanged(
        rotationHorizontal, rotationVertical
      ));

      // Update camera basis for movement calculations
      gameRepo.SetCameraBasis(camera.CameraBasis);

      // Follow player position directly (no spring arm for first-person)
      var transform = camera.GlobalTransform;
      transform.Origin = data.TargetPosition + camera.Offset;
      var globalTransform = camera.GlobalTransform.InterpolateWith(
        transform, (float)input.Delta * settings.FollowSpeed
      ).Orthonormalized();

      Output(new Output.GlobalTransformChanged(globalTransform));

      // Camera position is fixed at origin of gimbal system for first-person
      Output(new Output.CameraLocalPositionChanged(Vector3.Zero));

      return ToSelf();
    }

    public Transition On(in Input.TargetPositionChanged input)
    {
      var data = Get<Data>();
      data.TargetPosition = input.TargetPosition;
      return ToSelf();
    }
  }
}

