// ModularWeaponConfig.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "Weapon/Modular Config")]
public class ModularWeaponConfig : ScriptableObject
{
    [Header("=== WEAPON IDENTITY ===")]
    public string weaponName = "Burst Rifle";
    [Tooltip("Custom weapon type - add any string you want")]
    public string weaponType = "AssaultRifle"; // String instead of enum for extensibility

    [Header("=== AMMO SETTINGS ===")]
    public int magazineCapacity = 30;
    public int totalAmmo = 180;
    public float reloadTime = 2f;

    [Header("=== DAMAGE SETTINGS ===")]
    public float baseDamage = 25f;
    public ProjectileType projectileType = ProjectileType.Physical;

    [Header("=== FIRE SETTINGS ===")]
    [Tooltip("Time between shots (lower = faster)")]
    public float fireRate = 0.1f;
    [Tooltip("Number of shots in a burst")]
    public int burstCount = 3;
    [Tooltip("Time between shots in a burst")]
    public float burstInterval = 0.1f;
    [Tooltip("Time between bursts")]
    public float burstCooldown = 0.3f;

    [Header("=== PROJECTILE SETTINGS ===")]
    public float bulletVelocity = 200f;
    public float bulletLifetime = 3f;

    [Header("=== ACCURACY SETTINGS ===")]
    [Range(0f, 1f)]
    public float baseAccuracy = 0.8f;
    public float spreadAngle = 2f;

    [Header("=== AIMING SETTINGS ===")]
    public bool canAim = true;
    public float aimFOV = 40f;
    public float aimAccuracyMultiplier = 0.5f;
    public Vector3 aimPositionOffset = new Vector3(0, 0.05f, 0.2f);

    [Header("=== AUDIO SETTINGS ===")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip modeSwitchSound;

    [Header("=== TRAIL SETTINGS ===")]
    public bool enableTrails = true;
    public Color trailColor = Color.yellow;
    public float trailWidth = 0.1f;
    public float trailLength = 0.5f;

    [Header("=== SPECIAL PROPERTIES ===")]
    public bool penetratesTargets = false;
    public int maxPenetrations = 0;
    public ShieldBypassType[] bypassTypes = new ShieldBypassType[0];

    // === CALCULATED PROPERTIES ===
    public float GetBurstDPS() => (baseDamage * burstCount) / (burstInterval * (burstCount - 1) + burstCooldown);
    public float GetSingleDPS() => baseDamage / fireRate;

    // === PRESETS (Right-click in Inspector) ===
    [ContextMenu("Preset: Assault Rifle")]
    public void PresetAssaultRifle()
    {
        weaponName = "Assault Rifle";
        weaponType = "AssaultRifle";
        magazineCapacity = 30;
        totalAmmo = 180;
        baseDamage = 25f;
        fireRate = 0.1f;
        burstCount = 3;
        burstInterval = 0.1f;
        burstCooldown = 0.3f;
        bulletVelocity = 200f;
        bulletLifetime = 3f;
        spreadAngle = 2f;
        baseAccuracy = 0.8f;
    }

    [ContextMenu("Preset: Sniper Rifle")]
    public void PresetSniperRifle()
    {
        weaponName = "Sniper Rifle";
        weaponType = "SniperRifle";
        magazineCapacity = 5;
        totalAmmo = 25;
        baseDamage = 100f;
        fireRate = 1f;
        burstCount = 1;
        bulletVelocity = 400f;
        bulletLifetime = 5f;
        spreadAngle = 0.5f;
        baseAccuracy = 0.95f;
        aimFOV = 20f;
        penetratesTargets = true;
        maxPenetrations = 2;
    }

    [ContextMenu("Preset: Shotgun")]
    public void PresetShotgun()
    {
        weaponName = "Shotgun";
        weaponType = "Shotgun";
        magazineCapacity = 8;
        totalAmmo = 32;
        baseDamage = 15f; // Per pellet
        fireRate = 0.8f;
        burstCount = 8; // Number of pellets
        burstInterval = 0f; // Simultaneous
        bulletVelocity = 150f;
        bulletLifetime = 1f;
        spreadAngle = 15f;
        baseAccuracy = 0.3f;
    }

    [ContextMenu("Preset: Plasma Rifle")]
    public void PresetPlasmaRifle()
    {
        weaponName = "Plasma Rifle";
        weaponType = "PlasmaRifle"; // Custom type!
        magazineCapacity = 50;
        totalAmmo = 200;
        baseDamage = 30f;
        fireRate = 0.15f;
        burstCount = 1;
        bulletVelocity = 250f;
        bulletLifetime = 4f;
        spreadAngle = 1f;
        baseAccuracy = 0.9f;
        projectileType = ProjectileType.Plasma;
        trailColor = Color.blue;
    }

    [ContextMenu("Preset: Railgun")]
    public void PresetRailgun()
    {
        weaponName = "Railgun";
        weaponType = "Railgun"; // Another custom type!
        magazineCapacity = 1;
        totalAmmo = 10;
        baseDamage = 300f;
        fireRate = 3f;
        bulletVelocity = 1000f;
        bulletLifetime = 2f;
        spreadAngle = 0f;
        baseAccuracy = 1f;
        projectileType = ProjectileType.Tachyon;
        penetratesTargets = true;
        maxPenetrations = 10;
        trailColor = Color.cyan;
    }

    private void OnValidate()
    {
        // Clamp values
        magazineCapacity = Mathf.Max(1, magazineCapacity);
        totalAmmo = Mathf.Max(0, totalAmmo);
        baseDamage = Mathf.Max(0.1f, baseDamage);
        fireRate = Mathf.Max(0.01f, fireRate);
        burstCount = Mathf.Max(1, burstCount);
        burstInterval = Mathf.Max(0f, burstInterval);
        burstCooldown = Mathf.Max(0.01f, burstCooldown);
        bulletVelocity = Mathf.Max(1f, bulletVelocity);
        bulletLifetime = Mathf.Max(0.1f, bulletLifetime);
        baseAccuracy = Mathf.Clamp01(baseAccuracy);
        spreadAngle = Mathf.Max(0f, spreadAngle);
    }
}