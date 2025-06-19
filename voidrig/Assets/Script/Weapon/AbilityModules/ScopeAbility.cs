//ScopeAbility.cs
using UnityEngine;

public class ScopeAbility : BaseAbilityModule
{
    [Header("Scope Settings")]
    public float scopeZoomFOV = 20f;
    public float accuracyBonus = 0.2f;

    private Camera playerCamera;
    private float originalFOV;

    public override void Initialize(ModularWeapon weapon)
    {
        base.Initialize(weapon);
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            originalFOV = playerCamera.fieldOfView;
        }
    }

    protected override void OnAbilityActivated()
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = scopeZoomFOV;
        }

        weapon.SetAnimationTrigger("ScopeUp");

        // Create scope overlay UI if needed
        if (abilityData.abilityEffect != null)
        {
            Instantiate(abilityData.abilityEffect);
        }
    }

    protected override void OnAbilityDeactivated()
    {
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFOV;
        }

        weapon.SetAnimationTrigger("ScopeDown");
    }
}
//end