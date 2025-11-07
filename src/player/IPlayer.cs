namespace GameDemo;

using Chickensoft.GodotNodeInterfaces;
using Godot;

public interface IPlayerLogic
{
  object Value { get; }
}

public interface IPlayer :
  ICharacterBody3D, IKillable, ICoinCollector, IPushEnabled
{
  IPlayerLogic PlayerLogic { get; }

  bool IsMovingHorizontally();

  Vector3 GetGlobalInputVector(Basis cameraBasis);

  Basis GetNextRotationBasis(
    Vector3 direction,
    double delta,
    float rotationSpeed
  );
}

