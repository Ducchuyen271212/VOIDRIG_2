//HomingAbility.cs
using UnityEngine;

public class HomingAbility : BaseAbilityModule
{
    [Header("Homing Settings")]
    public float homingRange = 50f;
    public LayerMask enemyLayers = -1;

    private Transform currentTarget;

    protected override void OnAbilityActivated()
    {
        weapon.SetAnimationTrigger("HomingActivate");

        // Find nearest enemy
        FindNearestTarget();
    }

    private void FindNearestTarget()
    {
        Collider[] enemies = Physics.OverlapSphere(weapon.transform.position, homingRange, enemyLayers);
        float closestDistance = float.MaxValue;
        Transform closestEnemy = null;

        foreach (var enemy in enemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(weapon.transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy.transform;
                }
            }
        }

        currentTarget = closestEnemy;
    }

    public Transform GetHomingTarget()
    {
        return isActive ? currentTarget : null;
    }
}
//end