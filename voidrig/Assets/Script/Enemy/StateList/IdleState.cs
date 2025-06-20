using UnityEngine;

public class IdleState : EnemyState
{
    private Vector3 targetPos;
    private Vector3 direction;
    public IdleState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {

    }

    public override void AnimationTrigger()
    {
        base.AnimationTrigger();
    }

    public override void EnterState()
    {
        base.EnterState();

        targetPos = GetRandomPoint();

        Debug.Log("Entering Idle State");
    }

    public override void ExitState()
    {
        base.ExitState();

        Debug.Log("Exiting Idle State");
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        if (enemy.IsAggro)
        {
            enemy.StateMachine.ChangeState(enemy.ChaseState);
            Debug.Log("I'm Not Idle!!");
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        //Debug.Log($"Enemy {enemy.name} is in Idle State. Moving towards {targetPos}");

        direction = (targetPos - enemy.transform.position).normalized;
        direction.y = 0f; // Ensure movement is only on XZ

        enemy.Move(direction * enemy.IdleMovementSpeed);

        if (Vector3.Distance(enemy.transform.position, targetPos) < 0.1f)
        {
            targetPos = GetRandomPoint();
        }
    }

    public Vector3 GetRandomPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * enemy.RandomMovementRange;
        Vector3 randomPoint = enemy.transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        return randomPoint;
    }
}
