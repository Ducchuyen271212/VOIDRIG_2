//PlasmaProjectileModule.cs
using UnityEngine;

public class PlasmaProjectileModule : BaseProjectileModule
{
    [Header("Plasma Settings")]
    public float energyDecay = 0.1f;
    public bool burnsThroughArmor = true;

    public override GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        GameObject projectile = InstantiateProjectile(position, direction, velocity);

        if (projectile != null)
        {
            var bullet = projectile.GetComponent<ModularBullet>();
            bullet?.SetPlasmaProperties(energyDecay, burnsThroughArmor);

            // Add plasma effects
            AddPlasmaEffects(projectile);
        }

        return projectile;
    }

    private void AddPlasmaEffects(GameObject projectile)
    {
        // Add particle system for plasma trail
        var particles = projectile.GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            var main = particles.main;
            main.startColor = projectileData.projectileColor;
            main.startLifetime = 2f;
        }

        // Add light component for glow
        Light plasmaLight = projectile.GetComponent<Light>();
        if (plasmaLight == null)
        {
            plasmaLight = projectile.AddComponent<Light>();
        }
        plasmaLight.color = projectileData.projectileColor;
        plasmaLight.intensity = 2f;
        plasmaLight.range = 5f;
    }
}
//end