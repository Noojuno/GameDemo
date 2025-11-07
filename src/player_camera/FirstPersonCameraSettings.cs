namespace GameDemo;

using Godot;

[GlobalClass]
public partial class FirstPersonCameraSettings : Resource
{
  [Export(PropertyHint.Range, "0, 10, 0.01")]
  public float MouseSensitivity { get; set; } = 0.2f;

  [Export(PropertyHint.Range, "0, 10, 0.01")]
  public float JoypadSensitivity { get; set; } = 5;

  /// <summary>
  /// Vertical gimbal angle maximum constraint (in degrees).
  /// </summary>
  [Export(PropertyHint.Range, "0, 89.9, 0.01")]
  public float VerticalMax { get; set; } = 89.9f;

  /// <summary>
  /// Vertical gimbal angle minimum constraint (in degrees).
  /// </summary>
  [Export(PropertyHint.Range, "-89.9, -0.01, 0.01")]
  public float VerticalMin { get; set; } = -89.9f;

  /// <summary>
  /// How fast the camera follows the target (units per second).
  /// </summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float FollowSpeed = 20f;

  /// <summary>
  /// Acceleration for rotation applied to vertical gimbal.
  /// </summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float VerticalRotationAcceleration = 10f;

  /// <summary>
  /// Acceleration for rotation applied to horizontal gimbal.
  /// </summary>
  [Export(PropertyHint.Range, "0, 100, 0.1")]
  public float HorizontalRotationAcceleration = 10f;
}

