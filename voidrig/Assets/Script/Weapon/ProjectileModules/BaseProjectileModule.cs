// BaseProjectileModule.cs
using UnityEngine;

public abstract class BaseProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public ProjectileType projectileType = ProjectileType.Physical;
    public float damage = 25f;
    public float velocity = 100f;
    public float lifetime = 5f;

    protected ModularWeapon weapon;

    public virtual void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
    }

    public virtual void OnWeaponActivated() { }
    public virtual void OnWeaponDeactivated() { }
    public virtual void OnUpdate() { }

    public virtual GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (projectilePrefab == null) return null;

        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));

        // Set damage
        var bullet = projectile.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = damage;
        }

        var modularBullet = projectile.GetComponent<ModularCompatibleBullet>();
        if (modularBullet != null)
        {
            modularBullet.damage = damage;
            modularBullet.projectileType = projectileType;
        }

        // Apply velocity
        var rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * velocity;
        }

        // Destroy after lifetime
        Destroy(projectile, lifetime);

        return projectile;
    }

    public ProjectileType GetProjectileType() => projectileType;
}
// end