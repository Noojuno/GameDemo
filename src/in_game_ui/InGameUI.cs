namespace GameDemo;

using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

public interface IInGameUI : IControl
{
  void SetCoinsLabel(int coins, int totalCoins);
}

[Meta(typeof(IAutoNode))]
public partial class InGameUI : Control, IInGameUI
{
  public override void _Notification(int what) => this.Notify(what);

  #region Dependencies

  [Dependency] public IAppRepo AppRepo => this.DependOn<IAppRepo>();
  [Dependency] public IGameRepo GameRepo => this.DependOn<IGameRepo>();
  [Dependency] public EntityTable EntityTable => this.DependOn<EntityTable>();

  #endregion Dependencies

  #region Nodes

  [Node] public ILabel CoinsLabel { get; set; } = default!;
  [Node("%StateLabel")] public ILabel StateLabel { get; set; } = default!;

  #endregion Nodes

  #region State

  public IInGameUILogic InGameUILogic { get; set; } = default!;

  public InGameUILogic.IBinding InGameUIBinding { get; set; } = default!;

  private FirstPersonPlayerLogic? _firstPersonPlayerLogic;
  private string _lastStateText = string.Empty;

  #endregion State

  public void Setup() => InGameUILogic = new InGameUILogic();

  public void OnResolved()
  {
    InGameUILogic.Set(this);
    InGameUILogic.Set(AppRepo);
    InGameUILogic.Set(GameRepo);

    InGameUIBinding = InGameUILogic.Bind();

    InGameUIBinding
      .Handle((in InGameUILogic.Output.NumCoinsChanged output) =>
        SetCoinsLabel(
          output.NumCoinsCollected, output.NumCoinsAtStart
        )
      );

    InGameUILogic.Start();

    UpdatePlayerLogic();
  }

  public override void _Process(double delta)
  {
    if (_firstPersonPlayerLogic != null)
    {
      UpdateStateLabel();
    }
  }

  private void UpdatePlayerLogic()
  {
    var player = EntityTable.Get<IPlayer>("Player");
    if (player is FirstPersonPlayer firstPersonPlayer)
    {
      _firstPersonPlayerLogic = firstPersonPlayer.FirstPersonPlayerLogic;
      UpdateStateLabel();
    }
  }

  private void UpdateStateLabel()
  {
    if (_firstPersonPlayerLogic == null)
    {
      return;
    }
    
    var state = _firstPersonPlayerLogic.Value;
    var stateText = FormatFirstPersonStateName(state);
    
    if (stateText != _lastStateText)
    {
      StateLabel.Text = $"State: {stateText}";
      _lastStateText = stateText;
    }
  }

  private static string FormatFirstPersonStateName(FirstPersonPlayerLogic.State state)
  {
    return state switch
    {
      FirstPersonPlayerLogic.State.Disabled => "Disabled",
      FirstPersonPlayerLogic.State.Dead => "Dead",
      FirstPersonPlayerLogic.State.Alive.Grounded.Moving.Sprinting => "Sprinting",
      FirstPersonPlayerLogic.State.Alive.Grounded.Crouching.IdleState => "Crouching (Idle)",
      FirstPersonPlayerLogic.State.Alive.Grounded.Crouching.MovingState => "Crouching (Moving)",
      FirstPersonPlayerLogic.State.Alive.Grounded.Dashing => "Dashing",
      FirstPersonPlayerLogic.State.Alive.Grounded.Idle => "Idle",
      FirstPersonPlayerLogic.State.Alive.Grounded.Moving => "Moving",
      FirstPersonPlayerLogic.State.Alive.Airborne.Jumping => "Jumping",
      FirstPersonPlayerLogic.State.Alive.Airborne.Falling => "Falling",
      FirstPersonPlayerLogic.State.Alive.Airborne.Liftoff => "Liftoff",
      _ => FormatStateNameFallback(state)
    };
  }

  private static string FormatStateNameFallback(object state)
  {
    var typeName = state.GetType().Name;
    typeName = typeName.Replace("State", "");
    typeName = typeName.Replace("Alive", "");
    typeName = typeName.Replace("Grounded", "");
    typeName = typeName.Replace("Airborne", "");
    typeName = typeName.Replace("Crouching", "");
    typeName = typeName.Replace("Moving", "");
    typeName = typeName.Trim('.');
    return string.IsNullOrWhiteSpace(typeName) ? "Unknown" : typeName;
  }

  public void SetCoinsLabel(int coins, int totalCoins) =>
    CoinsLabel.Text = $"{coins}/{totalCoins}";

  public void OnExitTree()
  {
    InGameUILogic.Stop();
    InGameUIBinding.Dispose();
  }
}
