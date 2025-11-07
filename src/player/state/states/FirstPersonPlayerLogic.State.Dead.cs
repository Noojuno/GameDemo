namespace GameDemo;

using Chickensoft.Introspection;

public partial class FirstPersonPlayerLogic
{
  public abstract partial record State
  {
    [Meta, Id("first_person_player_logic_state_dead")]
    public partial record Dead : State;
  }
}

