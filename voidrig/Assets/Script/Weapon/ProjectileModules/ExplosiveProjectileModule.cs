//ExplosiveProjectileModule.cs
using UnityEngine;

public class ExplosiveProjectileModule : BaseProjectileModule
{
    [Header("Explosive Settings")]
    public float explosionRadius = 5f;
    public float explosionDamage = 50f;
    public GameObject explosionEffect;

    public override GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        GameObject projectile = InstantiateProjectile(position, direction, velocity);

        if (projectile != null)
        {
            var bullet = projectile.GetComponent<ModularBullet>();
            bullet?.SetExplosiveProperties(explosionRadius, explosionDamage, explosionEffect);
        }

        return projectile;
    }
}
//end