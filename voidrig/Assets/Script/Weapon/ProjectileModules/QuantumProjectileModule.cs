//QuantumProjectileModule.cs
using UnityEngine;

public class QuantumProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Quantum Projectile Settings")]
    public GameObject projectilePrefab;
    public float damage = 45f;
    public float velocity = 200f;
    public float lifetime = 3f;

    [Header("Quantum Properties")]
    public bool quantumTunneling = true;
    public float probabilityOfExistence = 0.8f;
    public int quantumStates = 3;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log("QuantumProjectileModule initialized");
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    public GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (projectilePrefab == null) return null;

        // Create multiple quantum states
        GameObject[] quantumStates = new GameObject[this.quantumStates];

        for (int i = 0; i < this.quantumStates; i++)
        {
            Vector3 quantumPosition = position + Random.insideUnitSphere * 0.5f;
            quantumStates[i] = Instantiate(projectilePrefab, quantumPosition, Quaternion.LookRotation(direction));

            if (quantumStates[i] != null)
            {
                // Set damage
                var bullet = quantumStates[i].GetComponent<Bullet>();
                if (bullet != null) bullet.damage = damage;

                var modularBullet = quantumStates[i].GetComponent<ModularCompatibleBullet>();
                if (modularBullet != null)
                {
                    modularBullet.damage = damage;
                    modularBullet.projectileType = ProjectileType.Quantum;
                }

                var fullBullet = quantumStates[i].GetComponent<ModularBullet>();
                if (fullBullet != null)
                {
                    fullBullet.SetQuantumProperties(quantumTunneling, probabilityOfExistence, i);
                }

                // Apply velocity
                var rb = quantumStates[i].GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = direction * velocity;

                // Make partially transparent
                var renderer = quantumStates[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    color.a = 0.6f;
                    renderer.material.color = color;
                }

                Destroy(quantumStates[i], lifetime);
            }
        }

        return quantumStates[0]; // Return primary state
    }

    public ProjectileType GetProjectileType() => ProjectileType.Quantum;
}

//end