using UnityEngine;

public interface ITriggerable
{
    bool IsAggro { get; set; }
    bool IsInAttackRange { get; set; }

    void setAggroStatus(bool isAggroed);
    void setAttackStatus(bool isAttacking);
}
