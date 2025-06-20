//PlasmaProjectileModule.cs
using UnityEngine;

public class PlasmaProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Plasma Projectile Settings")]
    public GameObject projectilePrefab;
    public float damage = 35f;
    public float velocity = 120f;
    public float lifetime = 3f;
    public Color plasmaColor = Color.magenta;

    [Header("Plasma Properties")]
    public float energyDecay = 0.1f;
    public bool burnsThroughArmor = true;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("PlasmaProjectileModule initialized");
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
            modularBullet.projectileType = ProjectileType.Plasma;
        }

        var fullBullet = projectile.GetComponent<ModularBullet>();
        if (fullBullet != null)
        {
            fullBullet.SetPlasmaProperties(energyDecay, burnsThroughArmor);
        }

        // Apply velocity
        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direction * velocity;

        // Add plasma effects
        AddPlasmaEffects(projectile);

        Destroy(projectile, lifetime);
        return projectile;
    }

    private void AddPlasmaEffects(GameObject projectile)
    {
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = plasmaColor;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", plasmaColor * 3f);
        }

        // Add plasma glow
        var light = projectile.GetComponent<Light>();
        if (light == null) light = projectile.AddComponent<Light>();
        light.color = plasmaColor;
        light.intensity = 3f;
        light.range = 4f;
    }

    public ProjectileType GetProjectileType() => ProjectileType.Plasma;
}
//end