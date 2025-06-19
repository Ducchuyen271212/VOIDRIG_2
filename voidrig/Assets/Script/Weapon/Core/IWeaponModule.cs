//IWeaponModule.cs
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Base interface for all weapon modules
public interface IWeaponModule
{
    void Initialize(ModularWeapon weapon);
    void OnWeaponActivated();
    void OnWeaponDeactivated();
    void OnUpdate();
}

// Interface for firing mechanisms
public interface IFireModule : IWeaponModule
{
    bool CanFire();
    IEnumerator Fire();
    void OnFireInput(bool isPressed, bool wasPressed);
}

// Interface for projectile types
public interface IProjectileModule : IWeaponModule
{
    GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity);
    ProjectileType GetProjectileType();
}

// Interface for weapon abilities
public interface IAbilityModule : IWeaponModule
{
    bool CanActivate();
    void ActivateAbility();
    float GetCooldownRemaining();
    string GetAbilityName();
}

// Interface for ammo systems
public interface IAmmoModule : IWeaponModule
{
    int GetCurrentAmmo();
    int GetTotalAmmo();
    bool ConsumeAmmo(int amount = 1);
    bool CanReload();
    IEnumerator Reload();
    void AddAmmo(int amount);
}

// Interface for targeting systems
public interface ITargetingModule : IWeaponModule
{
    Vector3 CalculateDirection(Vector3 baseDirection);
    bool HasTarget();
    Transform GetCurrentTarget();
}

// Interface for objects that can take damage
public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo);
    bool HasShield(ShieldBypassType shieldType);
}

public enum ProjectileType
{
    Physical,
    Plasma,
    Explosive,
    Tachyon,
    Exotic,
    Energy,
    Antimatter,
    Quantum
}

public enum ShieldBypassType
{
    None,
    PhysicalShield,
    EnergyShield,
    AllShields,
    Walls,
    Armor
}

public enum FireMode
{
    Single,
    Burst,
    Auto,
    Charge
}

public enum ShieldType
{
    Physical,
    Energy
}

[System.Serializable]
public class ProjectileData
{
    [Header("Basic Properties")]
    public ProjectileType type;
    public float damage = 10f;
    public float velocity = 100f;
    public float lifetime = 5f;

    [Header("Special Properties")]
    public ShieldBypassType[] bypassTypes;
    public bool hasSplash = false;
    public float splashRadius = 0f;
    public float splashDamage = 0f;

    [Header("Visual Effects")]
    public GameObject projectilePrefab;
    public GameObject impactEffect;
    public GameObject muzzleEffect;
    public Color projectileColor = Color.white;

    [Header("Physics")]
    public bool affectedByGravity = false;
    public bool penetratesTargets = false;
    public int maxPenetrations = 0;
}

[System.Serializable]
public class AbilityData
{
    public string abilityName;
    public float cooldown = 5f;
    public float duration = 0f;
    public bool requiresTarget = false;
    public GameObject abilityEffect;
}

[System.Serializable]
public class DamageInfo
{
    public float damage;
    public ProjectileType projectileType;
    public ShieldBypassType[] bypassTypes;
    public Vector3 hitPoint;
    public Vector3 hitNormal;
    public float chargeLevel = 1f;
    public bool isCriticalHit = false;

    public bool CanBypass(ShieldBypassType shieldType)
    {
        if (bypassTypes == null) return false;
        foreach (var bypass in bypassTypes)
        {
            if (bypass == shieldType || bypass == ShieldBypassType.AllShields)
                return true;
        }
        return false;
    }
}
// end