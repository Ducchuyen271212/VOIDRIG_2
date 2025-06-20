using UnityEngine;

public class ChaseState : EnemyState
{

    private Vector3 targetPos;
    private Vector3 direction;

    public ChaseState(Enemy enemy, EnemyStateMachine stateMachine) : base(enemy, stateMachine)
    {

    }

    public override void AnimationTrigger()
    {
        base.AnimationTrigger();
    }

    public override void EnterState()
    {
        base.EnterState();

        Debug.Log("Entering Chase State");
    }

    public override void ExitState()
    {
        base.ExitState();

        Debug.Log("Exiting Chase State");
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();

        //Debug.Log("Found Player!!");

        if (!enemy.IsAggro)
        {
            enemy.StateMachine.ChangeState(enemy.IdleState);
            Debug.Log("I'm Not Chasing!!");
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        targetPos = getPlayerPosition();
        direction = (targetPos - enemy.transform.position).normalized;

        enemy.Move(direction * enemy.IdleMovementSpeed);

        if (Vector3.Distance(enemy.transform.position, targetPos) < 0.1f)
        {
            targetPos = getPlayerPosition();
        }
    }

    public Vector3 getPlayerPosition()
    {
               
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }
        return Vector3.zero; 
    }
}
