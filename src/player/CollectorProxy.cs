namespace GameDemo;

using Chickensoft.AutoInject;
using Chickensoft.Collections;
using Chickensoft.GodotNodeInterfaces;
using Chickensoft.Introspection;
using Godot;

public interface ICollectorProxy : INode3D, ICoinCollector
{
  int PeerId { get; }
  void UpdatePosition(Vector3 position);
}

[Meta(typeof(IAutoNode))]
public partial class CollectorProxy : Node3D, ICollectorProxy
{
  public override void _Notification(int what) => this.Notify(what);

  #region Dependencies

  [Dependency]
  public EntityTable EntityTable => this.DependOn<EntityTable>();

  #endregion Dependencies

  public int PeerId { get; private set; }
  public Vector3 CenterOfMass => GlobalPosition + new Vector3(0f, 1f, 0f);

  public void Setup()
  {
  }

  public void OnReady()
  {
  }

  public void OnResolved()
  {
    EntityTable.Set(Name, this);
  }

  public void Initialize(int peerId)
  {
    PeerId = peerId;
    Name = $"CollectorProxy_{peerId}";
  }

  public void UpdatePosition(Vector3 position)
  {
    GlobalPosition = position;
  }

  public void OnExitTree()
  {
    EntityTable.Remove(Name);
  }
}

