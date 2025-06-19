using UnityEngine;

public interface IEnemyMovable
{
    Rigidbody RB { get; set; }

    bool IsMoving { get; set; }

    void Move(Vector3 direction);

    void Rotate(Vector3 direction, float speed);
}
