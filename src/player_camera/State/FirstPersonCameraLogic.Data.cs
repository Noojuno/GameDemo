namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.Serialization;
using Godot;

public partial class FirstPersonCameraLogic
{
  /// <summary>First-person camera data.</summary>
  [Meta, Id("first_person_camera_logic_data")]
  public partial record Data
  {
    /// <summary>
    /// The camera's target position. The camera will move towards this
    /// position each tick, allowing for smooth camera follow motion.
    /// </summary>
    [Save("target_position")]
    public required Vector3 TargetPosition { get; set; }

    /// <summary>Target horizontal angle (determined by input).</summary>
    [Save("target_angle_horizontal")]
    public required float TargetAngleHorizontal { get; set; }

    /// <summary>Target vertical angle (determined by input).</summary>
    [Save("target_angle_vertical")]
    public required float TargetAngleVertical { get; set; }
  }
}

