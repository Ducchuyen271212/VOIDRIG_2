using System;
using UnityEngine;

public class Enemy : MonoBehaviour, IDamagable, IEnemyMovable, ITriggerable
{
    [field: SerializeField] public float maxHealth { get; set; } = 10f;
    public float currentHealth { get; set; }
    public Rigidbody RB { get; set; }
    public bool IsMoving { get; set; }

    #region State Machine
    public EnemyStateMachine StateMachine { get; set; }
    public IdleState IdleState { get; set; }
    public ChaseState ChaseState { get; set; }
    public AttackState AttackState { get; set; }
    #endregion

    #region Idle State
    public float RandomMovementRange = 100f;
    public float IdleMovementSpeed = 10f;
    #endregion

    #region Chase State
    public bool IsAggro { get; set; }
    public bool IsInAttackRange { get; set; }
    #endregion

    private void Awake()
    {
        StateMachine = new EnemyStateMachine();

        IdleState = new IdleState(this, StateMachine);
        ChaseState = new ChaseState(this, StateMachine);
        AttackState = new AttackState(this, StateMachine);

        StateMachine.Initialize(IdleState);
    }

    private void Start()
    {
        currentHealth = maxHealth;

        RB = GetComponent<Rigidbody>();

        StateMachine.ChangeState(IdleState);
    }

    private void Update()
    {
        StateMachine.CurrentState.FrameUpdate();
        //Debug.Log("The current state is: " + StateMachine.CurrentState);
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    public void Damage(float dmgTaken)
    {
        currentHealth -= dmgTaken;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }

    public void Move(Vector3 direction)
    {
        Vector3 move = direction.normalized;
        RB.MovePosition(transform.position + move * Time.deltaTime * IdleMovementSpeed);
    }

    public void Rotate(Vector3 direction, float speed)
    {

    }

    public void setAggroStatus(bool isAggroed)
    {
        IsAggro = isAggroed;
    }

    public void setAttackStatus(bool isAttacking)
    {
        IsInAttackRange = isAttacking;
    }
}
