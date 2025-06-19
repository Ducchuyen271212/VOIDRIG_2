//ShieldAbility.cs
using UnityEngine;

public class ShieldAbility : BaseAbilityModule
{
    [Header("Shield Settings")]
    public float shieldStrength = 100f;
    public ShieldType shieldType = ShieldType.Energy;

    private GameObject shieldObject;

    protected override void OnAbilityActivated()
    {
        // Create shield object
        if (abilityData.abilityEffect != null)
        {
            shieldObject = Instantiate(abilityData.abilityEffect, weapon.transform.parent);

            Shield shieldComponent = shieldObject.GetComponent<Shield>();
            if (shieldComponent == null)
            {
                shieldComponent = shieldObject.AddComponent<Shield>();
            }

            shieldComponent.shieldType = shieldType;
            shieldComponent.shieldStrength = shieldStrength;
            shieldComponent.maxShieldStrength = shieldStrength;
        }

        weapon.SetAnimationTrigger("ShieldActivate");
    }

    protected override void OnAbilityDeactivated()
    {
        if (shieldObject != null)
        {
            Destroy(shieldObject);
        }

        weapon.SetAnimationTrigger("ShieldDeactivate");
    }
}
//end