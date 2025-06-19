//AntimatterProjectileModule.cs
using UnityEngine;

public class AntimatterProjectileModule : BaseProjectileModule
{
    [Header("Antimatter Settings")]
    public float annihilationRadius = 3f;
    public bool destroysProjectiles = true;

    public override GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.LookRotation(direction));
        projectile.GetComponent<Rigidbody>().linearVelocity = direction.normalized * velocity;

        if (projectile != null)
        {
            var bullet = projectile.GetComponent<ModularBullet>();
            bullet?.SetAntimatterProperties(annihilationRadius, destroysProjectiles);

            AddAntimatterEffects(projectile);
        }

        return projectile;
    }

    private void AddAntimatterEffects(GameObject projectile)
    {
        // Add containment field effects
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", Color.white * 3f);
        }

        // Add dangerous energy field
        Light antimatterLight = projectile.GetComponent<Light>();
        if (antimatterLight == null)
        {
            antimatterLight = projectile.AddComponent<Light>();
        }
        antimatterLight.color = Color.white;
        antimatterLight.intensity = 5f;
        antimatterLight.range = annihilationRadius;
    }
}
//end
