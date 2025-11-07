namespace GameDemo;

using Chickensoft.Introspection;

public partial class FirstPersonCameraLogic
{
  public partial record State
  {
    /// <summary>The state of the first-person camera when input is disabled.</summary>
    [Meta, Id("first_person_camera_logic_state_input_disabled")]
    public partial record InputDisabled : State,
    IGet<Input.EnableInput>
    {
      public Transition On(in Input.EnableInput input) => To<InputEnabled>();
    }
  }
}

