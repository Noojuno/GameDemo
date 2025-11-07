namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class FirstPersonPlayerLogic
{
  public partial record State
  {
    [Meta, Id("first_person_player_logic_state_alive_airborne_falling")]
    public partial record Falling : Airborne
    {
      public Falling()
      {
        this.OnEnter(() => Output(new Output.Animations.Fall()));
      }
    }
  }
}

