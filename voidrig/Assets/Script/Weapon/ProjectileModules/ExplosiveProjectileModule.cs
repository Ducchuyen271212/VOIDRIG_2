//ExplosiveProjectileModule.cs
using UnityEngine;

public class ExplosiveProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Explosive Projectile Settings")]
    public GameObject projectilePrefab;
    public float damage = 40f;
    public float velocity = 80f;
    public float lifetime = 5f;

    [Header("Explosive Properties")]
    public float explosionRadius = 5f;
    public float explosionDamage = 50f;
    public GameObject explosionEffect;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("ExplosiveProjectileModule initialized");
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
            modularBullet.projectileType = ProjectileType.Explosive;
        }

        var fullBullet = projectile.GetComponent<ModularBullet>();
        if (fullBullet != null)
        {
            fullBullet.SetExplosiveProperties(explosionRadius, explosionDamage, explosionEffect);
        }

        // Apply velocity
        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = direction * velocity;

        Destroy(projectile, lifetime);
        return projectile;
    }

    public ProjectileType GetProjectileType() => ProjectileType.Explosive;
}
//end