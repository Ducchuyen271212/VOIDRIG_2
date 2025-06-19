// HealthToDamageableAdapter.cs
using UnityEngine;

// This adapter allows your existing Health system to work with the IDamageable interface
[RequireComponent(typeof(Health))]
public class HealthToDamageableAdapter : MonoBehaviour, IDamageable
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (health != null)
        {
            health.TakeDamage(damageInfo.damage);
        }
    }

    public bool HasShield(ShieldBypassType shieldType)
    {
        // Check if this object has a shield component
        var shield = GetComponent<Shield>();
        if (shield != null)
        {
            switch (shieldType)
            {
                case ShieldBypassType.PhysicalShield:
                    return shield.shieldType == ShieldType.Physical;
                case ShieldBypassType.EnergyShield:
                    return shield.shieldType == ShieldType.Energy;
                case ShieldBypassType.AllShields:
                    return true;
            }
        }
        return false;
    }
}