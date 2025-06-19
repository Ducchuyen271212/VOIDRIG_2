// ShotgunFireModule.cs
using System.Collections;
using UnityEngine;

public class ShotgunFireModule : MonoBehaviour, IFireModule
{
    [Header("Shotgun Settings")]
    public int pelletCount = 8;
    public SpreadPattern spreadPattern = SpreadPattern.Horizontal;
    public float spreadAngle = 15f;
    public float choke = 1f; // 0 = wide spread, 1 = tight spread

    [Header("Advanced Patterns")]
    public bool useCustomPattern = false;
    public Vector2[] customSpreadOffsets; // For custom spread patterns

    public enum SpreadPattern
    {
        Circular,      // Standard circular spread
        Horizontal,    // Spread more horizontally (like a real shotgun)
        Vertical,      // Spread more vertically
        Cross,         // + pattern
        Star,          // * pattern
        Ring,          // Ring around center
        Cluster,       // Groups of pellets
        Random         // Completely random
    }

    private ModularWeapon weapon;
    private bool readyToShoot = true;
    private float lastShotTime;
    private bool isFiring = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
    }

    public void OnWeaponActivated()
    {
        readyToShoot = true;
        isFiring = false;
    }

    public void OnWeaponDeactivated()
    {
        isFiring = false;
    }

    public void OnUpdate() { }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        // Double check weapon is active before processing any input
        if (!weapon.isActiveWeapon)
        {
            Debug.Log("ShotgunFireModule: Weapon not active, ignoring fire input");
            return;
        }

        if (wasPressed && CanFire())
        {
            StartCoroutine(Fire());
        }
    }

    public bool CanFire()
    {
        // Must be active weapon to fire
        if (!weapon.isActiveWeapon)
        {
            return false;
        }

        if (weapon?.WeaponData == null)
        {
            return false;
        }

        var projectileModule = weapon.GetProjectileModule();
        if (projectileModule == null)
        {
            return false;
        }

        var ammoModule = weapon.GetAmmoModule();
        if (ammoModule == null)
        {
            return false;
        }

        // Check if reloading - CANNOT FIRE WHILE RELOADING
        var standardAmmo = ammoModule as StandardAmmoModule;
        if (standardAmmo != null && standardAmmo.IsReloading())
        {
            Debug.Log("Cannot fire - weapon is reloading");
            return false;
        }

        float fireCooldown = weapon.WeaponData.fireRate;
        bool canFireByRate = Time.time - lastShotTime >= fireCooldown;

        bool hasAmmo = ammoModule.GetCurrentAmmo() > 0;

        return readyToShoot && canFireByRate && hasAmmo && !isFiring;
    }

    public IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        isFiring = true;
        readyToShoot = false;
        lastShotTime = Time.time;

        var projectileModule = weapon.GetProjectileModule();
        var targetingModule = weapon.GetTargetingModule();
        var ammoModule = weapon.GetAmmoModule();

        // Check if we have ammo
        if (ammoModule != null && ammoModule.GetCurrentAmmo() <= 0)
        {
            // Play empty sound
            if (weapon.WeaponSound?.emptyClip != null)
            {
                weapon.PlaySound(weapon.WeaponSound.emptyClip);
            }
            weapon.SetAnimationTrigger("RecoilRecover");
            isFiring = false;
            readyToShoot = true;
            yield break;
        }

        if (projectileModule != null && ammoModule != null)
        {
            // Check if projectile module can create projectiles
            var physicalProjectile = projectileModule as PhysicalProjectileModule;
            if (physicalProjectile != null && physicalProjectile.projectilePrefab == null)
            {
                Debug.LogError("PhysicalProjectileModule has no projectile prefab assigned!");

                // Try to find a bullet prefab in the weapon
                var bulletPrefab = weapon.GetComponent<Weapon>()?.bulletPrefab;
                if (bulletPrefab != null)
                {
                    physicalProjectile.projectilePrefab = bulletPrefab;
                    Debug.Log($"Found bullet prefab: {bulletPrefab.name}");
                }
                else
                {
                    Debug.LogError("No bullet prefab found! Cannot fire.");
                    isFiring = false;
                    readyToShoot = true;
                    yield break;
                }
            }

            Vector3 baseDirection = weapon.CalculateBaseDirection();

            // Use ShotgunFireModule's pellet count instead of weapon data for shotguns
            int actualPellets = pelletCount; // Always use our module's pellet count

            // Only fallback to weapon data if our pellet count is invalid
            if (actualPellets <= 0)
            {
                actualPellets = weapon.WeaponData?.bulletsPerBurst ?? 8;
            }

            Debug.Log($"Firing shotgun with {actualPellets} pellets, pattern: {spreadPattern}");

            // Generate spread directions based on pattern
            Vector3[] spreadDirections = GenerateSpreadDirections(baseDirection, actualPellets);

            Debug.Log($"Generated {spreadDirections.Length} spread directions");

            // Fire all pellets
            int successfulPellets = 0;
            for (int i = 0; i < actualPellets && i < spreadDirections.Length; i++)
            {
                Vector3 spreadDirection = spreadDirections[i];
                Vector3 finalDirection = targetingModule?.CalculateDirection(spreadDirection) ?? spreadDirection;

                Debug.Log($"Firing pellet {i + 1}/{actualPellets}: direction = {finalDirection}");

                GameObject projectile = projectileModule.CreateProjectile(
                    weapon.FirePoint.position,
                    finalDirection,
                    weapon.WeaponData.bulletVelocity
                );

                if (projectile != null)
                {
                    successfulPellets++;

                    // Don't disable renderers for the simplified system - let trails show
                    Debug.Log($"✓ Created shotgun pellet {successfulPellets}/{actualPellets}");
                }
                else
                {
                    Debug.LogError($"✗ Failed to create pellet {i + 1}!");
                }
            }

            Debug.Log($"Shotgun fired: {successfulPellets}/{actualPellets} pellets created successfully");

            // Consume one shot worth of ammo
            bool ammoConsumed = ammoModule.ConsumeAmmo(1);
            Debug.Log($"Shotgun fired! Ammo consumed: {ammoConsumed}, remaining: {ammoModule.GetCurrentAmmo()}");

            // Play effects
            weapon.SetAnimationTrigger("Recoil");
            weapon.PlaySound(weapon.WeaponSound?.shootClip);

            // Create muzzle flash if available
            CreateMuzzleFlash();
        }

        // Wait for fire cooldown
        float cooldown = weapon.WeaponData?.fireRate ?? 0.8f;
        yield return new WaitForSeconds(cooldown);

        // Recovery
        weapon.SetAnimationTrigger("RecoilRecover");
        readyToShoot = true;
        isFiring = false;
    }

    private Vector3[] GenerateSpreadDirections(Vector3 baseDirection, int pelletCount)
    {
        Vector3[] directions = new Vector3[pelletCount];

        if (useCustomPattern && customSpreadOffsets != null && customSpreadOffsets.Length >= pelletCount)
        {
            // Use custom pattern
            for (int i = 0; i < pelletCount; i++)
            {
                directions[i] = ApplySpreadOffset(baseDirection, customSpreadOffsets[i]);
            }
            return directions;
        }

        // Calculate actual spread based on weapon data and choke
        float actualSpread = weapon.WeaponData?.spreadIntensity ?? spreadAngle;
        actualSpread *= choke; // Apply choke modifier

        // Apply aiming accuracy if available
        if (AimingManager.Instance != null && AimingManager.Instance.isAiming)
        {
            actualSpread *= AimingManager.Instance.GetAccuracyMultiplier();
        }

        switch (spreadPattern)
        {
            case SpreadPattern.Circular:
                return GenerateCircularSpread(baseDirection, pelletCount, actualSpread);

            case SpreadPattern.Horizontal:
                return GenerateHorizontalSpread(baseDirection, pelletCount, actualSpread);

            case SpreadPattern.Vertical:
                return GenerateVerticalSpread(baseDirection, pelletCount, actualSpread);

            case SpreadPattern.Cross:
                return GenerateCrossSpread(baseDirection, pelletCount, actualSpread);

            case SpreadPattern.Star:
                return GenerateStarSpread(baseDirection, pelletCount, actualSpread);

            case SpreadPattern.Ring:
                return GenerateRingSpread(baseDirection, pelletCount, actualSpread);

            case SpreadPattern.Cluster:
                return GenerateClusterSpread(baseDirection, pelletCount, actualSpread);

            case SpreadPattern.Random:
                return GenerateRandomSpread(baseDirection, pelletCount, actualSpread);

            default:
                return GenerateCircularSpread(baseDirection, pelletCount, actualSpread);
        }
    }

    private Vector3[] GenerateCircularSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f); // Random angle
            float distance = Random.Range(0.1f, 1f) * spread; // Random distance from center

            // Convert to spread angles (much larger values for visible spread)
            float horizontalSpread = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
            float verticalSpread = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;

            directions[i] = ApplySpreadOffset(baseDirection, new Vector2(horizontalSpread, verticalSpread));
        }

        return directions;
    }

    private Vector3[] GenerateHorizontalSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            // Spread pellets horizontally with some vertical variation
            float horizontalSpread = Random.Range(-spread, spread);
            float verticalSpread = Random.Range(-spread * 0.5f, spread * 0.5f); // Less vertical

            directions[i] = ApplySpreadOffset(baseDirection, new Vector2(horizontalSpread, verticalSpread));
        }

        return directions;
    }

    private Vector3[] GenerateVerticalSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            // Spread pellets vertically with some horizontal variation
            float verticalSpread = Random.Range(-spread, spread);
            float horizontalSpread = Random.Range(-spread * 0.5f, spread * 0.5f); // Less horizontal

            directions[i] = ApplySpreadOffset(baseDirection, new Vector2(horizontalSpread, verticalSpread));
        }

        return directions;
    }

    private Vector3[] GenerateCrossSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Vector2 offset;

            // Create cross pattern with random variation
            if (i % 4 == 0) offset = new Vector2(Random.Range(spread * 0.3f, spread), 0); // Right
            else if (i % 4 == 1) offset = new Vector2(Random.Range(-spread, -spread * 0.3f), 0); // Left
            else if (i % 4 == 2) offset = new Vector2(0, Random.Range(spread * 0.3f, spread)); // Up
            else offset = new Vector2(0, Random.Range(-spread, -spread * 0.3f)); // Down

            // Add randomness
            offset.x += Random.Range(-spread * 0.3f, spread * 0.3f);
            offset.y += Random.Range(-spread * 0.3f, spread * 0.3f);

            directions[i] = ApplySpreadOffset(baseDirection, offset);
        }

        return directions;
    }

    private Vector3[] GenerateStarSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i; // Even distribution
            float distance = Random.Range(spread * 0.5f, spread); // Vary distance

            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );

            directions[i] = ApplySpreadOffset(baseDirection, offset);
        }

        return directions;
    }

    private Vector3[] GenerateRingSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        // One pellet in center (with slight randomness)
        directions[0] = ApplySpreadOffset(baseDirection, Random.insideUnitCircle * spread * 0.1f);

        // Rest in a ring
        for (int i = 1; i < count; i++)
        {
            float angle = (360f / (count - 1)) * (i - 1);
            float distance = Random.Range(spread * 0.7f, spread); // Ring with some variation

            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance
            );

            directions[i] = ApplySpreadOffset(baseDirection, offset);
        }

        return directions;
    }

    private Vector3[] GenerateClusterSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        // Create 2-3 clusters
        int clusters = Mathf.Min(3, Mathf.Max(2, count / 3));
        int pelletsPerCluster = count / clusters;
        int pelletIndex = 0;

        for (int cluster = 0; cluster < clusters && pelletIndex < count; cluster++)
        {
            // Cluster center
            float clusterAngle = Random.Range(0f, 360f);
            float clusterDistance = Random.Range(spread * 0.3f, spread * 0.8f);
            Vector2 clusterCenter = new Vector2(
                Mathf.Cos(clusterAngle * Mathf.Deg2Rad) * clusterDistance,
                Mathf.Sin(clusterAngle * Mathf.Deg2Rad) * clusterDistance
            );

            // Pellets in cluster
            int pelletsInThisCluster = Mathf.Min(pelletsPerCluster + Random.Range(-1, 2), count - pelletIndex);
            for (int p = 0; p < pelletsInThisCluster; p++)
            {
                Vector2 clusterOffset = Random.insideUnitCircle * spread * 0.2f;
                Vector2 finalOffset = clusterCenter + clusterOffset;

                directions[pelletIndex] = ApplySpreadOffset(baseDirection, finalOffset);
                pelletIndex++;
            }
        }

        // Fill remaining slots if any
        while (pelletIndex < count)
        {
            Vector2 offset = Random.insideUnitCircle * spread;
            directions[pelletIndex] = ApplySpreadOffset(baseDirection, offset);
            pelletIndex++;
        }

        return directions;
    }

    private Vector3[] GenerateRandomSpread(Vector3 baseDirection, int count, float spread)
    {
        Vector3[] directions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spread;
            directions[i] = ApplySpreadOffset(baseDirection, offset);
        }

        return directions;
    }

    private Vector3 ApplySpreadOffset(Vector3 baseDirection, Vector2 offset)
    {
        // Multiply by a larger factor to make spread more visible
        float spreadMultiplier = 3f; // Increase this to make spread more noticeable
        offset *= spreadMultiplier;

        // Get the camera's right and up vectors for proper world-space rotation
        Vector3 cameraRight = Camera.main.transform.right;
        Vector3 cameraUp = Camera.main.transform.up;

        // Apply horizontal and vertical spread in world space
        Vector3 spreadDirection = baseDirection + (cameraRight * offset.x) + (cameraUp * offset.y);

        Debug.Log($"ApplySpreadOffset: input offset: {offset}, multiplied: {offset * spreadMultiplier}, final direction: {spreadDirection.normalized}");

        return spreadDirection.normalized;
    }

    private void CreateMuzzleFlash()
    {
        // Look for muzzle effect on weapon
        Transform muzzle = weapon.FirePoint.Find("MuzzleEffect");
        if (muzzle != null)
        {
            ParticleSystem muzzlePS = muzzle.GetComponent<ParticleSystem>();
            if (muzzlePS != null)
            {
                muzzlePS.Play();
            }
        }

        // Create additional shotgun-specific effects
        CreateShotgunBlast();
    }

    private void CreateShotgunBlast()
    {
        // Create a wider muzzle flash effect for shotgun
        GameObject blastEffect = new GameObject("ShotgunBlast");
        blastEffect.transform.position = weapon.FirePoint.position;
        blastEffect.transform.rotation = weapon.FirePoint.rotation;

        // Add particle system for shotgun blast
        ParticleSystem ps = blastEffect.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.2f;
        main.startSpeed = 10f;
        main.startSize = 0.5f;
        main.startColor = Color.yellow;
        main.maxParticles = 50;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spreadAngle * 2f; // Wider than normal muzzle flash

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 50)
        });

        // Destroy after effect
        Destroy(blastEffect, 1f);
    }
}
// end