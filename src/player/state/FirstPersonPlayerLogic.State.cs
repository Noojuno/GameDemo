namespace GameDemo;

using Chickensoft.Introspection;
using Chickensoft.LogicBlocks;

public partial class FirstPersonPlayerLogic
{
  [Meta]
  public abstract partial record State : StateLogic<State>;
}

