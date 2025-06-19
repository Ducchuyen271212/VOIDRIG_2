// ModularBullet.cs - Fixed with null checks
using System;
using UnityEngine;
using System.Collections.Generic;

public class ModularBullet : MonoBehaviour
{
    [Header("Current State")]
    public ProjectileData projectileData;
    public GunData.Attribute weaponData;

    private float damageMultiplier = 1f;
    private float chargeLevel = 1f;
    private int penetrationsLeft = 0;

    // Special properties for different projectile types
    private bool hasPlasmaProperties = false;
    private float plasmaEnergyDecay = 0f;
    private bool burnsThroughArmor = false;

    private bool hasExplosiveProperties = false;
    private float explosionRadius = 0f;
    private float explosionDamage = 0f;
    private GameObject explosionEffect;

    private bool hasTachyonProperties = false;
    private float phaseShiftChance = 0f;

    private bool hasExoticProperties = false;
    private bool hasNegativeMass = false;
    private float gravityReversalStrength = 0f;
    private bool phasesToDifferentDimension = false;

    private bool hasEnergyProperties = false;
    private bool overloadsShields = false;
    private float energyDrainAmount = 0f;

    private bool hasAntimatterProperties = false;
    private float annihilationRadius = 0f;
    private bool destroysProjectiles = false;

    private bool hasQuantumProperties = false;
    private bool quantumTunneling = false;
    private float probabilityOfExistence = 1f;
    private int quantumStateId = 0;

    // Cache components
    private Renderer cachedRenderer;
    private Collider cachedCollider;

    private void Start()
    {
        // Cache components
        cachedRenderer = GetComponent<Renderer>();
        cachedCollider = GetComponent<Collider>();

        if (projectileData != null)
        {
            penetrationsLeft = projectileData.maxPenetrations;

            // Apply gravity settings
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = projectileData.affectedByGravity && !hasExoticProperties;
            }
        }
    }

    private void Update()
    {
        // Handle quantum probability
        if (hasQuantumProperties && UnityEngine.Random.Range(0f, 1f) > probabilityOfExistence)
        {
            // Quantum decoherence - bullet temporarily doesn't exist
            if (cachedCollider != null)
                cachedCollider.enabled = false;
            if (cachedRenderer != null)
                cachedRenderer.enabled = false;
        }
        else
        {
            if (cachedCollider != null)
                cachedCollider.enabled = true;
            if (cachedRenderer != null)
                cachedRenderer.enabled = true;
        }

        // Handle plasma energy decay
        if (hasPlasmaProperties && plasmaEnergyDecay > 0)
        {
            damageMultiplier -= plasmaEnergyDecay * Time.deltaTime;
            if (damageMultiplier <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    public void Initialize(ProjectileData data, GunData.Attribute weaponData)
    {
        this.projectileData = data;
        this.weaponData = weaponData;
        if (data != null)
        {
            penetrationsLeft = data.maxPenetrations;
        }
    }

    // === PROPERTY SETTERS ===

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = multiplier;
    }

    public void SetChargeLevel(float charge)
    {
        chargeLevel = charge;
    }

    public void SetPlasmaProperties(float energyDecay, bool burnArmor)
    {
        hasPlasmaProperties = true;
        plasmaEnergyDecay = energyDecay;
        burnsThroughArmor = burnArmor;
    }

    public void SetExplosiveProperties(float radius, float damage, GameObject effect)
    {
        hasExplosiveProperties = true;
        explosionRadius = radius;
        explosionDamage = damage;
        explosionEffect = effect;
    }

    public void SetTachyonProperties(float phaseChance)
    {
        hasTachyonProperties = true;
        phaseShiftChance = phaseChance;
    }

    public void SetExoticProperties(bool negativeMass, float gravityStrength, bool dimensionPhase)
    {
        hasExoticProperties = true;
        hasNegativeMass = negativeMass;
        gravityReversalStrength = gravityStrength;
        phasesToDifferentDimension = dimensionPhase;
    }

    public void SetEnergyProperties(bool shieldOverload, float energyDrain)
    {
        hasEnergyProperties = true;
        overloadsShields = shieldOverload;
        energyDrainAmount = energyDrain;
    }

    public void SetAntimatterProperties(float radius, bool destroysProjectiles)
    {
        hasAntimatterProperties = true;
        annihilationRadius = radius;
        this.destroysProjectiles = destroysProjectiles;
    }

    public void SetQuantumProperties(bool tunneling, float probability, int stateId)
    {
        hasQuantumProperties = true;
        quantumTunneling = tunneling;
        probabilityOfExistence = probability;
        quantumStateId = stateId;
    }

    // === COLLISION HANDLING ===

    private void OnCollisionEnter(Collision collision)
    {
        GameObject hitObject = collision.gameObject;
        ContactPoint contact = collision.contacts[0];

        // === IGNORE ALL WEAPONS ===
        if (IsWeapon(hitObject))
        {
            Debug.Log($"ModularBullet ignoring weapon: {hitObject.name}");
            return; // Don't process collision with weapons
        }

        // Check for special bypasses
        if (ShouldBypassCollision(hitObject))
        {
            return; // Phase through
        }

        // Handle different target types
        bool shouldDestroy = true;

        if (hitObject.CompareTag("Enemy") || hitObject.CompareTag("Player"))
        {
            shouldDestroy = HandleLivingTargetHit(hitObject, contact);
        }
        else if (hitObject.CompareTag("Wall") || hitObject.CompareTag("Obstacle"))
        {
            shouldDestroy = HandleEnvironmentHit(hitObject, contact);
        }
        else if (hitObject.CompareTag("Projectile"))
        {
            shouldDestroy = HandleProjectileHit(hitObject, contact);
        }

        // Handle penetration
        if (projectileData != null && projectileData.penetratesTargets && penetrationsLeft > 0)
        {
            penetrationsLeft--;
            shouldDestroy = false;
        }

        // Create impact effects
        CreateImpactEffect(contact);

        // Handle explosions
        if (hasExplosiveProperties)
        {
            CreateExplosion(contact.point);
            shouldDestroy = true;
        }

        // Handle antimatter annihilation
        if (hasAntimatterProperties)
        {
            CreateAnnihilation(contact.point);
            shouldDestroy = true;
        }

        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
    }

    private bool ShouldBypassCollision(GameObject hitObject)
    {
        // Tachyon phase shifting
        if (hasTachyonProperties && UnityEngine.Random.Range(0f, 1f) < phaseShiftChance)
        {
            return true;
        }

        // Quantum tunneling
        if (hasQuantumProperties && quantumTunneling)
        {
            float tunnelChance = 0.3f;
            if (hitObject.CompareTag("Wall"))
            {
                return UnityEngine.Random.Range(0f, 1f) < tunnelChance;
            }
        }

        // Exotic dimensional phasing
        if (hasExoticProperties && phasesToDifferentDimension)
        {
            if (UnityEngine.Random.Range(0f, 1f) < 0.2f)
            {
                return true;
            }
        }

        // Check bypass types
        if (projectileData?.bypassTypes != null)
        {
            foreach (var bypassType in projectileData.bypassTypes)
            {
                if (CanBypassTarget(hitObject, bypassType))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CanBypassTarget(GameObject target, ShieldBypassType bypassType)
    {
        switch (bypassType)
        {
            case ShieldBypassType.Walls:
                return target.CompareTag("Wall");
            case ShieldBypassType.PhysicalShield:
                var shield = target.GetComponent<Shield>();
                return shield != null && shield.shieldType == ShieldType.Physical;
            case ShieldBypassType.EnergyShield:
                var energyShield = target.GetComponent<Shield>();
                return energyShield != null && energyShield.shieldType == ShieldType.Energy;
            case ShieldBypassType.AllShields:
                return target.GetComponent<Shield>() != null;
            case ShieldBypassType.Armor:
                return target.GetComponent<Armor>() != null;
        }
        return false;
    }

    private bool HandleLivingTargetHit(GameObject target, ContactPoint contact)
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            DamageInfo damageInfo = CreateDamageInfo(contact);
            damageable.TakeDamage(damageInfo);
        }

        return true; // Destroy bullet after hitting living target
    }

    private bool HandleEnvironmentHit(GameObject target, ContactPoint contact)
    {
        // Some projectiles can destroy environment
        if (hasAntimatterProperties || (hasExplosiveProperties && explosionDamage > 100f))
        {
            var destructible = target.GetComponent<DestructibleEnvironment>();
            if (destructible != null && projectileData != null)
            {
                destructible.TakeDamage(projectileData.damage * damageMultiplier);
            }
        }

        return true; // Usually destroy on environment hit
    }

    private bool HandleProjectileHit(GameObject otherProjectile, ContactPoint contact)
    {
        if (hasAntimatterProperties && destroysProjectiles)
        {
            Destroy(otherProjectile);
            return true;
        }

        // Some projectiles bounce off each other
        if (projectileData != null && projectileData.type == ProjectileType.Physical)
        {
            return false; // Don't destroy, let physics handle it
        }

        return true;
    }

    private DamageInfo CreateDamageInfo(ContactPoint contact)
    {
        float baseDamage = projectileData?.damage ?? 10f;
        float finalDamage = baseDamage * damageMultiplier * chargeLevel;

        // Apply weapon-specific damage bonuses
        if (weaponData != null)
        {
            finalDamage *= weaponData.damage / 10f; // Normalize weapon damage
        }

        return new DamageInfo
        {
            damage = finalDamage,
            projectileType = projectileData?.type ?? ProjectileType.Physical,
            bypassTypes = projectileData?.bypassTypes,
            hitPoint = contact.point,
            hitNormal = contact.normal,
            chargeLevel = chargeLevel,
            isCriticalHit = UnityEngine.Random.Range(0f, 1f) < 0.1f // 10% crit chance
        };
    }

    private void CreateImpactEffect(ContactPoint contact)
    {
        if (projectileData?.impactEffect != null)
        {
            GameObject effect = Instantiate(
                projectileData.impactEffect,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );

            // Set effect color based on projectile type
            SetEffectColor(effect);

            Destroy(effect, 2f);
        }
        else if (GlobalReferences.Instance?.bulletImpactEffectPrefab != null)
        {
            GameObject hole = Instantiate(
                GlobalReferences.Instance.bulletImpactEffectPrefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            hole.transform.SetParent(contact.otherCollider.transform);
        }
        else if (GlobalReferences.Instance?.bulletImpactEffectPrefab != null) // Handle typo in your code
        {
            GameObject hole = Instantiate(
                GlobalReferences.Instance.bulletImpactEffectPrefab,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            hole.transform.SetParent(contact.otherCollider.transform);
        }
    }

    private void SetEffectColor(GameObject effect)
    {
        if (effect == null || projectileData == null) return;

        var particles = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (var particle in particles)
        {
            if (particle != null)
            {
                var main = particle.main;
                main.startColor = projectileData.projectileColor;
            }
        }
    }

    private void CreateExplosion(Vector3 position)
    {
        // Deal area damage
        Collider[] hitColliders = Physics.OverlapSphere(position, explosionRadius);

        foreach (var collider in hitColliders)
        {
            var damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector3.Distance(position, collider.transform.position);
                float damageRatio = 1f - (distance / explosionRadius);

                DamageInfo explosionDamageInfo = new DamageInfo
                {
                    damage = explosionDamage * damageRatio,
                    projectileType = projectileData?.type ?? ProjectileType.Explosive,
                    bypassTypes = projectileData?.bypassTypes,
                    hitPoint = position,
                    hitNormal = Vector3.up
                };

                damageable.TakeDamage(explosionDamageInfo);
            }
        }

        // Create explosion effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, position, Quaternion.identity);
        }
    }

    private void CreateAnnihilation(Vector3 position)
    {
        // Antimatter annihilation - destroys everything in radius
        Collider[] hitColliders = Physics.OverlapSphere(position, annihilationRadius);

        foreach (var collider in hitColliders)
        {
            if (collider.CompareTag("Projectile") && destroysProjectiles)
            {
                Destroy(collider.gameObject);
            }

            var damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                DamageInfo annihilationDamage = new DamageInfo
                {
                    damage = 999999f, // Massive damage
                    projectileType = ProjectileType.Antimatter,
                    bypassTypes = new ShieldBypassType[] { ShieldBypassType.AllShields },
                    hitPoint = position,
                    hitNormal = Vector3.up
                };

                damageable.TakeDamage(annihilationDamage);
            }
        }

        // Create annihilation effect
        CreateAnnihilationEffect(position);
    }

    private void CreateAnnihilationEffect(Vector3 position)
    {
        // Create massive explosion-like effect
        GameObject effect = new GameObject("AnnihilationEffect");
        effect.transform.position = position;

        // Add light
        Light annihilationLight = effect.AddComponent<Light>();
        annihilationLight.color = Color.white;
        annihilationLight.intensity = 10f;
        annihilationLight.range = annihilationRadius * 2f;

        // Destroy after effect
        Destroy(effect, 1f);
    }

    // === WEAPON DETECTION ===
    private bool IsWeapon(GameObject obj)
    {
        // Check for weapon components
        if (obj.GetComponent<ModularWeapon>() != null) return true;
        if (obj.GetComponent<Weapon>() != null) return true;

        // Check for weapon tags
        if (obj.CompareTag("MachineGun")) return true;
        if (obj.CompareTag("ShotGun")) return true;
        if (obj.CompareTag("Sniper")) return true;
        if (obj.CompareTag("HandGun")) return true;
        if (obj.CompareTag("SMG")) return true;
        if (obj.CompareTag("BurstRifle")) return true;

        // Check name patterns (common weapon naming)
        string objName = obj.name.ToLower();
        if (objName.Contains("weapon") ||
            objName.Contains("gun") ||
            objName.Contains("rifle") ||
            objName.Contains("pistol") ||
            objName.Contains("shotgun") ||
            objName.Contains("sniper") ||
            objName.Contains("test")) return true;

        return false;
    }
}