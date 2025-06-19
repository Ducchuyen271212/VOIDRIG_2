// ManualTargetingModule.cs
using System;
using UnityEngine;

public class ManualTargetingModule : MonoBehaviour, ITargetingModule
{

    [Header("Accuracy Settings")]
    public float baseSpread = 1f;
    public AnimationCurve accuracyCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);

    private ModularWeapon weapon;
    private float aimTime = 0f;
    private bool isAiming = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }

    public void OnUpdate()
    {
        // Track aiming time for accuracy improvement
        if (AimingManager.Instance != null)
        {
            if (AimingManager.Instance.isAiming)
            {
                if (!isAiming)
                {
                    isAiming = true;
                    aimTime = 0f;
                }
                else
                {
                    aimTime += Time.deltaTime;
                }
            }
            else
            {
                isAiming = false;
                aimTime = 0f;
            }
        }
    }

    public Vector3 CalculateDirection(Vector3 baseDirection)
    {
        float spread = baseSpread;

        // Apply weapon accuracy
        if (weapon.WeaponData != null)
        {
            float weaponAccuracy = weapon.WeaponData.accuracy;
            spread *= (2f - weaponAccuracy) * weapon.WeaponData.spreadIntensity;
        }

        // Apply aiming bonus
        if (isAiming && AimingManager.Instance != null)
        {
            float aimAccuracy = AimingManager.Instance.GetAccuracyMultiplier();
            spread *= aimAccuracy;

            // Improve accuracy over time when aiming
            float timeBonus = accuracyCurve.Evaluate(Mathf.Min(aimTime / 2f, 1f));
            spread *= timeBonus;
        }

        // Apply spread
        Quaternion spreadRotation = Quaternion.Euler(
            UnityEngine.Random.Range(-spread, spread),
            UnityEngine.Random.Range(-spread, spread),
            0
        );

        return spreadRotation * baseDirection;
    }

    public bool HasTarget() => false;
    public Transform GetCurrentTarget() => null;
}
// end