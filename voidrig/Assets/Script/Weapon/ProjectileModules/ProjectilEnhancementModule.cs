//ProjectileEnhancementModule.cs
using UnityEngine;

public class ProjectileEnhancementModule : MonoBehaviour, IWeaponModule
{
    [Header("Damage Modifications")]
    public float damageMultiplier = 1f;
    public float additionalDamage = 0f;

    [Header("Speed Modifications")]
    public float speedMultiplier = 1f;
    public float additionalSpeed = 0f;

    [Header("Lifetime Modifications")]
    public float lifetimeMultiplier = 1f;
    public float additionalLifetime = 0f;

    [Header("Special Properties")]
    public bool makeBulletsGlow = false;
    public Color glowColor = Color.yellow;
    public bool addTrailEffect = false;
    public Material trailMaterial;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;

        // Hook into projectile creation
        var projectileModule = weapon.GetProjectileModule();
        if (projectileModule != null)
        {
            Debug.Log($"ProjectileEnhancementModule initialized - Damage: x{damageMultiplier}, Speed: x{speedMultiplier}");
        }
    }

    public void OnWeaponActivated()
    {
        Debug.Log($"Projectile enhancements active: +{additionalDamage} damage, +{additionalSpeed} speed");
    }

    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    // Methods for other modules to call
    public float GetEnhancedDamage(float baseDamage)
    {
        return (baseDamage * damageMultiplier) + additionalDamage;
    }

    public float GetEnhancedSpeed(float baseSpeed)
    {
        return (baseSpeed * speedMultiplier) + additionalSpeed;
    }

    public float GetEnhancedLifetime(float baseLifetime)
    {
        return (baseLifetime * lifetimeMultiplier) + additionalLifetime;
    }

    public void EnhanceProjectile(GameObject projectile)
    {
        if (projectile == null) return;

        // Enhance basic bullet
        var bullet = projectile.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = GetEnhancedDamage(bullet.damage);
        }

        // Enhance modular bullet
        var modularBullet = projectile.GetComponent<ModularBullet>();
        if (modularBullet != null && modularBullet.projectileData != null)
        {
            modularBullet.projectileData.damage = GetEnhancedDamage(modularBullet.projectileData.damage);
            modularBullet.projectileData.velocity = GetEnhancedSpeed(modularBullet.projectileData.velocity);
            modularBullet.projectileData.lifetime = GetEnhancedLifetime(modularBullet.projectileData.lifetime);
        }

        // Add visual enhancements
        if (makeBulletsGlow)
        {
            AddGlowEffect(projectile);
        }

        if (addTrailEffect)
        {
            AddTrailEffect(projectile);
        }
    }

    private void AddGlowEffect(GameObject projectile)
    {
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", glowColor);
        }

        // Add light component
        var light = projectile.GetComponent<Light>();
        if (light == null)
        {
            light = projectile.AddComponent<Light>();
        }
        light.color = glowColor;
        light.intensity = 2f;
        light.range = 3f;
    }

    private void AddTrailEffect(GameObject projectile)
    {
        var trail = projectile.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = projectile.AddComponent<TrailRenderer>();
        }

        if (trailMaterial != null)
        {
            trail.material = trailMaterial;
        }
        else
        {
            // Create default trail material
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = glowColor;
        }

        trail.time = 0.3f;
        trail.startWidth = 0.1f;
        trail.endWidth = 0.01f;
        trail.startColor = glowColor;
        trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
    }

    // Preset configurations
    [ContextMenu("Preset: High Damage")]
    public void PresetHighDamage()
    {
        damageMultiplier = 2f;
        additionalDamage = 10f;
        speedMultiplier = 1f;
        additionalSpeed = 0f;
        makeBulletsGlow = true;
        glowColor = Color.red;
    }

    [ContextMenu("Preset: High Speed")]
    public void PresetHighSpeed()
    {
        damageMultiplier = 1f;
        additionalDamage = 0f;
        speedMultiplier = 2f;
        additionalSpeed = 50f;
        makeBulletsGlow = true;
        glowColor = Color.cyan;
        addTrailEffect = true;
    }

    [ContextMenu("Preset: Long Range")]
    public void PresetLongRange()
    {
        damageMultiplier = 1f;
        additionalDamage = 0f;
        speedMultiplier = 1.5f;
        additionalSpeed = 25f;
        lifetimeMultiplier = 3f;
        makeBulletsGlow = false;
        addTrailEffect = true;
    }
}
// end