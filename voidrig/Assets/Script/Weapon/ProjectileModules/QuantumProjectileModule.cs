//QuantumProjectileModule.cs
using UnityEngine;

public class QuantumProjectileModule : BaseProjectileModule
{
    [Header("Quantum Settings")]
    public bool quantumTunneling = true;
    public float probabilityOfExistence = 0.8f;
    public int quantumStates = 3;

    public override GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        // Create multiple quantum states
        GameObject[] quantumStates = new GameObject[this.quantumStates];

        for (int i = 0; i < this.quantumStates; i++)
        {
            Vector3 quantumPosition = position + Random.insideUnitSphere * 0.5f;
            quantumStates[i] = InstantiateProjectile(quantumPosition, direction, velocity);

            if (quantumStates[i] != null)
            {
                var bullet = quantumStates[i].GetComponent<ModularBullet>();
                bullet?.SetQuantumProperties(quantumTunneling, probabilityOfExistence, i);

                // Make partially transparent
                var renderer = quantumStates[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    color.a = 0.6f;
                    renderer.material.color = color;
                }
            }
        }

        return quantumStates[0]; // Return primary state
    }
}
//end