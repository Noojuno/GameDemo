namespace GameDemo;

using System.Runtime.CompilerServices;
using Chickensoft.Introspection;
using Chickensoft.Serialization;
using Godot;

public partial class FirstPersonPlayerLogic
{
  /// <summary>Data shared between states.</summary>
  [Meta, Id("first_person_player_logic_data")]
  public partial record Data
  {
    [Save("last_strong_direction")]
    public Vector3 LastStrongDirection { get; set; } = Vector3.Forward;
    [Save("last_velocity")]
    public Vector3 LastVelocity { get; set; } = Vector3.Zero;
    [Save("was_on_floor")]
    public bool WasOnFloor { get; set; } = true;
    [Save("is_sprint_held")]
    public bool IsSprintHeld { get; set; }
    [Save("is_crouch_held")]
    public bool IsCrouchHeld { get; set; }
    [Save("dash_timer")]
    public float DashTimer { get; set; }
    [Save("dash_cooldown_timer")]
    public float DashCooldownTimer { get; set; }
    [Save("dash_direction")]
    public Vector3 DashDirection { get; set; } = Vector3.Zero;
    [Save("camera_offset")]
    public float CameraOffset { get; set; } = 0f;
    [Save("is_crouch_edge_blocked")]
    public bool IsCrouchEdgeBlocked { get; set; }
    [Save("last_crouch_edge_blocked")]
    public bool LastCrouchEdgeBlocked { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HadNegativeYVelocity() => LastVelocity.Y < 0f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool WasMovingHorizontally(Settings settings) =>
      LastCrouchEdgeBlocked ||
      (LastVelocity with { Y = 0f }).Length() >= settings.StoppingSpeed;
  }
}

