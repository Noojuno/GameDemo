namespace GameDemo;

using Godot;

public partial class FirstPersonCameraLogic
{
  public static class Input
  {
    public readonly record struct PhysicsTicked(double Delta);
    public readonly record struct MouseInputOccurred(
      InputEventMouseMotion Motion
    );
    public readonly record struct JoyPadInputOccurred(
      InputEventJoypadMotion Motion
    );
    public readonly record struct TargetPositionChanged(Vector3 TargetPosition);
    public readonly record struct EnableInput;
    public readonly record struct DisableInput;
  }
}

