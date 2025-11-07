namespace GameDemo;

public partial class FirstPersonPlayerLogic
{
  /// <summary>First-person player settings.</summary>
  /// <param name="StoppingSpeed">Stopping velocity (meters/sec).</param>
  /// <param name="Gravity">Player gravity (meters/sec).</param>
  /// <param name="MoveSpeed">Player speed (meters/sec).</param>
  /// <param name="Acceleration">Player acceleration (meters^2/sec).</param>
  /// <param name="JumpImpulseForce">Jump initial impulse force.</param>
  /// <param name="JumpForce">Additional force added each physics tick while
  /// player is still pressing jump.</param>
  /// <param name="SprintSpeedMultiplier">Multiplier applied while sprinting.</param>
  /// <param name="CrouchSpeedMultiplier">Multiplier applied while crouching.</param>
  /// <param name="DashSpeed">Horizontal dash speed (meters/sec).</param>
  /// <param name="DashDuration">Dash duration (seconds).</param>
  /// <param name="DashCooldown">Dash cooldown (seconds).</param>
  /// <param name="CrouchCameraOffset">Vertical camera offset while crouched.</param>
  /// <param name="CrouchLedgeProbeDistance">Horizontal distance to probe for ledge support while crouched.</param>
  /// <param name="CrouchLedgeVerticalTolerance">Vertical tolerance when checking for ledge support while crouched.</param>
  public record Settings(
    float StoppingSpeed,
    float Gravity,
    float MoveSpeed,
    float Acceleration,
    float JumpImpulseForce,
    float JumpForce,
    float SprintSpeedMultiplier,
    float CrouchSpeedMultiplier,
    float DashSpeed,
    float DashDuration,
    float DashCooldown,
    float CrouchCameraOffset,
    float CrouchLedgeProbeDistance,
    float CrouchLedgeVerticalTolerance
  );
}

