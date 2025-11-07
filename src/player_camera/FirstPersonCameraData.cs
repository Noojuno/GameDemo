namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.Serialization;
using Godot;

[Meta, Id("first_person_camera_data")]
public partial record FirstPersonCameraData
{
  [Save("state_machine")]
  public required IFirstPersonCameraLogic StateMachine { get; init; }

  [Save("global_transform")]
  public required Transform3D GlobalTransform { get; init; }

  [Save("local_position")]
  public required Vector3 LocalPosition { get; init; }
}

