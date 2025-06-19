//Armor.cs
using UnityEngine;

public class Armor : MonoBehaviour
{
    public float armorValue = 50f;
    public bool immuneToPlasma = false;

    public float CalculateDamageReduction(DamageInfo damageInfo)
    {
        // Plasma immunity
        if (immuneToPlasma && damageInfo.projectileType == ProjectileType.Plasma)
        {
            return 0f; // No damage
        }

        // Calculate damage reduction based on armor value
        float reduction = armorValue / (armorValue + 100f);
        return damageInfo.damage * (1f - reduction);
    }

    public bool IsImmuneToProjectile(ProjectileType projectileType)
    {
        return immuneToPlasma && projectileType == ProjectileType.Plasma;
    }
}
// end