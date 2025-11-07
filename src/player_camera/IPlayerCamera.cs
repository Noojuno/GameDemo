namespace GameDemo;

using Chickensoft.GodotNodeInterfaces;

public interface IPlayerCameraLogic
{
  object Value { get; }
}

public interface IPlayerCamera : INode3D
{
  IPlayerCameraLogic CameraLogic { get; }
  
  void UsePlayerCamera();
}

