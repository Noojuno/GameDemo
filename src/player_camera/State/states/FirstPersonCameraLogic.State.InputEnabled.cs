namespace GameDemo;

using Chickensoft.Introspection;
using Godot;

public partial class FirstPersonCameraLogic
{
  public partial record State
  {
    /// <summary>The state of the first-person camera when input is enabled.</summary>
    [Meta, Id("first_person_camera_logic_state_input_enabled")]
    public partial record InputEnabled : State,
    IGet<Input.DisableInput>,
    IGet<Input.MouseInputOccurred>,
    IGet<Input.JoyPadInputOccurred>
    {
      public Transition On(in Input.DisableInput input) => To<InputDisabled>();

      public Transition On(in Input.MouseInputOccurred input)
      {
        var settings = Get<FirstPersonCameraSettings>();
        var data = Get<Data>();

        var targetAngleVertical = Mathf.Clamp(
          data.TargetAngleVertical +
          (-input.Motion.Relative.Y * settings.MouseSensitivity),
          settings.VerticalMin,
          settings.VerticalMax
        );

        data.TargetAngleHorizontal +=
          -input.Motion.Relative.X * settings.MouseSensitivity;
        data.TargetAngleVertical = targetAngleVertical;

        return ToSelf();
      }

      public Transition On(in Input.JoyPadInputOccurred input)
      {
        var settings = Get<FirstPersonCameraSettings>();
        var data = Get<Data>();

        if (input.Motion.Axis == JoyAxis.RightX)
        {
          data.TargetAngleHorizontal +=
           -input.Motion.AxisValue * settings.JoypadSensitivity;
        }

        if (input.Motion.Axis == JoyAxis.RightY)
        {
          data.TargetAngleVertical = (float)Mathf.Clamp(
             data.TargetAngleVertical +
             (-input.Motion.AxisValue * settings.JoypadSensitivity),
             settings.VerticalMin,
             settings.VerticalMax
           );
        }
        return ToSelf();
      }
    }
  }
}

