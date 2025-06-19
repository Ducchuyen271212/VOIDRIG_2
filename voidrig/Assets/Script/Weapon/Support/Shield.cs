//Shield.cs - Remove namespace to avoid conflicts
using UnityEngine;

public class Shield : MonoBehaviour
{
    public ShieldType shieldType = ShieldType.Energy;
    public float shieldStrength = 100f;
    public float maxShieldStrength = 100f;

    [Header("Shield Behavior")]
    public float regenRate = 10f; // Shield regen per second
    public float regenDelay = 3f; // Delay before regen starts
    public bool autoRegen = true;

    private float lastDamageTime = -999f;

    private void Update()
    {
        if (autoRegen && shieldStrength < maxShieldStrength)
        {
            if (Time.time >= lastDamageTime + regenDelay)
            {
                RegenerateShield(regenRate * Time.deltaTime);
            }
        }
    }

    public bool CanBlock(DamageInfo damageInfo)
    {
        if (shieldStrength <= 0) return false;

        ShieldBypassType requiredBypass = shieldType == ShieldType.Physical
            ? ShieldBypassType.PhysicalShield
            : ShieldBypassType.EnergyShield;

        return !damageInfo.CanBypass(requiredBypass);
    }

    public float AbsorbDamage(DamageInfo damageInfo)
    {
        if (!CanBlock(damageInfo))
        {
            return damageInfo.damage; // All damage passes through
        }

        float damageAbsorbed = Mathf.Min(damageInfo.damage, shieldStrength);
        shieldStrength -= damageAbsorbed;
        lastDamageTime = Time.time;

        return damageInfo.damage - damageAbsorbed; // Return remaining damage
    }

    public void TakeDamage(float damage)
    {
        shieldStrength -= damage;
        shieldStrength = Mathf.Max(0, shieldStrength);
        lastDamageTime = Time.time;
    }

    public void RegenerateShield(float amount)
    {
        shieldStrength += amount;
        shieldStrength = Mathf.Min(shieldStrength, maxShieldStrength);
    }

    public bool IsActive()
    {
        return shieldStrength > 0;
    }

    public float GetShieldPercentage()
    {
        return shieldStrength / maxShieldStrength;
    }
}
// end