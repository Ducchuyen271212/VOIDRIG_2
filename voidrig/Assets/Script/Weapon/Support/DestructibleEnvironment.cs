//DestructibleEnvironment.cs
using UnityEngine;

public class DestructibleEnvironment : MonoBehaviour, IDamageable
{
    [Header("Destructible Settings")]
    public float health = 100f;
    public float maxHealth = 100f;

    [Header("Effects")]
    public GameObject destructionEffect;
    public GameObject[] debrisObjects;

    private void Start()
    {
        maxHealth = health;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        TakeDamage(damageInfo.damage);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;

        if (health <= 0)
        {
            DestroyEnvironment();
        }
    }

    public bool HasShield(ShieldBypassType shieldType)
    {
        return false; // Environment objects don't have shields
    }

    private void DestroyEnvironment()
    {
        // Create destruction effect
        if (destructionEffect != null)
        {
            Instantiate(destructionEffect, transform.position, transform.rotation);
        }

        // Spawn debris
        if (debrisObjects != null && debrisObjects.Length > 0)
        {
            foreach (var debris in debrisObjects)
            {
                if (debris != null)
                {
                    GameObject spawnedDebris = Instantiate(debris, transform.position, Random.rotation);

                    // Add some random force to debris
                    Rigidbody rb = spawnedDebris.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 randomForce = Random.insideUnitSphere * 5f + Vector3.up * 2f;
                        rb.AddForce(randomForce, ForceMode.Impulse);
                    }
                }
            }
        }

        Destroy(gameObject);
    }

    public void Repair(float amount)
    {
        health += amount;
        health = Mathf.Min(health, maxHealth);
    }

    public float GetHealthPercentage()
    {
        return health / maxHealth;
    }
}