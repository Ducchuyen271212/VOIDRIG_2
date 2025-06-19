using UnityEngine;

public interface IDamagable
{
    void Damage(float dmgTaken);

    void Die();

    float maxHealth { get; set; }

    float currentHealth { get; set; }
}
