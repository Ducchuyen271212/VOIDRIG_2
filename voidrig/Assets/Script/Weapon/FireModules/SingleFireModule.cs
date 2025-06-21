// SingleFireModule.cs
using UnityEngine;
using System.Collections;

public class SingleFireModule : MonoBehaviour, IFireModule
{
    [Header("Single Fire Settings")]
    public float fireRate = 0.3f;

    [Header("Accuracy Settings")]
    [Tooltip("Base scatter amount in degrees (0 = perfect accuracy)")]
    public float baseScatter = 0f;

    [Tooltip("Scatter increases with continuous fire")]
    public bool dynamicScatter = false;

    [Tooltip("Max scatter when firing continuously")]
    public float maxScatter = 5f;

    [Tooltip("How fast scatter increases")]
    public float scatterIncreaseRate = 2f;

    [Tooltip("How fast scatter recovers")]
    public float scatterRecoveryRate = 4f;

    private ModularWeapon weapon;
    private bool isFirePressed = false;
    private bool wasFirePressed = false;
    private float lastFireTime = -1f;
    private bool isFiring = false;
    private float currentScatter = 0f;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        currentScatter = baseScatter;
        Debug.Log($"SingleFireModule initialized - Base Scatter: {baseScatter}°");
    }

    public void OnWeaponActivated()
    {
        isFiring = false;
        currentScatter = baseScatter;
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
        currentScatter = baseScatter;
    }

    public void OnUpdate()
    {
        // Update scatter recovery
        if (dynamicScatter && !isFiring && Time.time > lastFireTime + 0.1f)
        {
            currentScatter = Mathf.MoveTowards(currentScatter, baseScatter, scatterRecoveryRate * Time.deltaTime);
        }
    }

    public bool CanFire()
    {
        bool hasAmmo = weapon.GetAmmoModule()?.GetCurrentAmmo() > 0;
        bool fireRateOK = Time.time >= lastFireTime + fireRate;
        return hasAmmo && fireRateOK && !isFiring;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        isFirePressed = isPressed;
        wasFirePressed = wasPressed;

        if (wasPressed && CanFire())
        {
            StartCoroutine(Fire());
        }
    }

    public IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        isFiring = true;
        lastFireTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        if (ammoModule != null && ammoModule.ConsumeAmmo())
        {
            Vector3 baseDirection = weapon.CalculateBaseDirection();

            // Apply scatter
            if (currentScatter > 0f)
            {
                baseDirection = ApplyScatter(baseDirection, currentScatter);
            }

            Vector3 finalDirection = targetingModule?.CalculateDirection(baseDirection) ?? baseDirection;

            projectileModule?.CreateProjectile(
                weapon.FirePoint.position,
                finalDirection,
                100f
            );

            // Increase scatter for next shot if dynamic
            if (dynamicScatter)
            {
                currentScatter = Mathf.Min(currentScatter + scatterIncreaseRate, maxScatter);
            }
        }

        yield return new WaitForSeconds(fireRate);
        isFiring = false;
    }

    private Vector3 ApplyScatter(Vector3 direction, float scatterAmount)
    {
        if (scatterAmount <= 0f) return direction;

        // Random scatter within cone
        float scatterX = Random.Range(-scatterAmount, scatterAmount);
        float scatterY = Random.Range(-scatterAmount, scatterAmount);

        Quaternion scatterRotation = Quaternion.Euler(scatterY, scatterX, 0);
        return scatterRotation * direction;
    }

    // Preset configurations
    [ContextMenu("Preset: Precision Rifle")]
    public void PresetPrecisionRifle()
    {
        fireRate = 0.3f;
        baseScatter = 0f;
        dynamicScatter = false;
    }

    [ContextMenu("Preset: Battle Rifle")]
    public void PresetBattleRifle()
    {
        fireRate = 0.25f;
        baseScatter = 0.5f;
        dynamicScatter = true;
        maxScatter = 3f;
        scatterIncreaseRate = 1f;
        scatterRecoveryRate = 3f;
    }

    [ContextMenu("Preset: Pistol")]
    public void PresetPistol()
    {
        fireRate = 0.2f;
        baseScatter = 1f;
        dynamicScatter = true;
        maxScatter = 5f;
        scatterIncreaseRate = 3f;
        scatterRecoveryRate = 2f;
    }
}
//end