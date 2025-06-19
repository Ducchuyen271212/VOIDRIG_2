//OverchargeAbility.cs
using UnityEngine;

public class OverchargeAbility : BaseAbilityModule
{
    [Header("Overcharge Settings")]
    public float damageMultiplier = 2f;
    public float fireRateMultiplier = 1.5f;
    public Color overchargeColor = Color.red;

    private Renderer weaponRenderer;
    private Color originalColor;

    public override void Initialize(ModularWeapon weapon)
    {
        base.Initialize(weapon);
        weaponRenderer = weapon.GetComponentInChildren<Renderer>();
        if (weaponRenderer != null)
        {
            originalColor = weaponRenderer.material.color;
        }
    }

    protected override void OnAbilityActivated()
    {
        weapon.SetAnimationTrigger("Overcharge");

        // Visual effects
        if (weaponRenderer != null)
        {
            weaponRenderer.material.color = overchargeColor;
            weaponRenderer.material.EnableKeyword("_EMISSION");
            weaponRenderer.material.SetColor("_EmissionColor", overchargeColor);
        }

        // Create overcharge effect
        if (abilityData.abilityEffect != null)
        {
            GameObject effect = Instantiate(abilityData.abilityEffect, weapon.transform);
            Destroy(effect, abilityData.duration);
        }
    }

    protected override void OnAbilityDeactivated()
    {
        // Restore original appearance
        if (weaponRenderer != null)
        {
            weaponRenderer.material.color = originalColor;
            weaponRenderer.material.DisableKeyword("_EMISSION");
        }
    }

    public float GetDamageMultiplier()
    {
        return isActive ? damageMultiplier : 1f;
    }

    public float GetFireRateMultiplier()
    {
        return isActive ? fireRateMultiplier : 1f;
    }
}
//end