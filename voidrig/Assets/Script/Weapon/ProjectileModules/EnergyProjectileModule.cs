//EnergyProjectileModule.cs
using UnityEngine;

public class EnergyProjectileModule : BaseProjectileModule
{
    [Header("Energy Settings")]
    public bool overloadsShields = true;
    public float energyDrainAmount = 25f;

    public override GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        GameObject projectile = InstantiateProjectile(position, direction, velocity);

        if (projectile != null)
        {
            var bullet = projectile.GetComponent<ModularBullet>();
            bullet?.SetEnergyProperties(overloadsShields, energyDrainAmount);

            AddEnergyEffects(projectile);
        }

        return projectile;
    }

    private void AddEnergyEffects(GameObject projectile)
    {
        // Add crackling energy effects
        var renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.blue;
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", Color.blue * 2f);
        }
    }
}
//end