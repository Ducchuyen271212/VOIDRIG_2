//EnergyProjectileModule.cs
using UnityEngine;

public class EnergyProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Energy Projectile Settings")]
    public GameObject projectilePrefab;
    public float damage = 30f;
    public float velocity = 150f;
    public float lifetime = 4f;
    public Color energyColor = Color.blue;

    [Header("Energy Properties")]
    public bool overloadsShields = true;
    public float energyDrainAmount = 25f;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("EnergyProjectileModule initialized");
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    public GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (projectilePrefab == null) return null;

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));

        // Set damage
        var bullet = projectile.GetComponent<Bullet>();
        if (bullet != null) bullet.damage = damage;

        var modularBullet = projectile.GetComponent<ModularCompatibleBullet>();
        if (modularBullet != null)
        {
            modularBullet.damage = damage;
            modularBullet.projectileType = ProjectileType.Energy;
        }

        var fullBullet = projectile.GetComponent<ModularBullet>();
        if (fullBullet != null)
        {
            fullBullet.SetEnergyProperties(overloadsShields, energyDrainAmount);
        }

        // Apply velocity
        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direction * velocity;

        // Add energy effects
        AddEnergyEffects(projectile);

        Destroy(projectile, lifetime);
        return projectile;
    }

    private void AddEnergyEffects(GameObject projectile)
    {
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = energyColor;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", energyColor * 2f);
        }

        // Add light
        var light = projectile.GetComponent<Light>();
        if (light == null) light = projectile.AddComponent<Light>();
        light.color = energyColor;
        light.intensity = 2f;
        light.range = 3f;
    }

    public ProjectileType GetProjectileType() => ProjectileType.Energy;
}
//end