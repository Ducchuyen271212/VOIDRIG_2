// ShotgunFireModule.cs
using System.Collections;
using UnityEngine;

public class ShotgunFireModule : MonoBehaviour, IFireModule
{
    [Header("Shotgun Settings")]
    public int pelletCount = 8;

    [Header("Spread Pattern")]
    [Tooltip("Maximum spread angle in degrees")]
    public float spreadAngle = 15f;

    [Tooltip("Spread pattern type")]
    public SpreadPattern spreadPattern = SpreadPattern.Circular;

    [Tooltip("For circular pattern - ensures even distribution")]
    public bool useGoldenRatio = true;

    [Header("Other Settings")]
    public float bulletLifetimeOverride = -1f;
    public float fireRate = 0.8f;

    private ModularWeapon weapon;
    private bool readyToShoot = true;
    private float lastShotTime;
    private bool isFiring = false;

    // Golden ratio for even distribution
    private const float GOLDEN_ANGLE = 137.5f;

    public enum SpreadPattern
    {
        Circular,      // Even circular distribution
        RandomCone,    // Random within cone
        Ring,          // Ring pattern
        Cross,         // Cross/plus pattern
        Star           // Star pattern
    }

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        Debug.Log($"ShotgunFireModule initialized - Pattern: {spreadPattern}");
    }

    public void OnWeaponActivated() { readyToShoot = true; isFiring = false; }
    public void OnWeaponDeactivated() => isFiring = false;
    public void OnUpdate() { }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        if (wasPressed && CanFire())
            StartCoroutine(Fire());
    }

    public bool CanFire()
    {
        if (Time.time < lastShotTime + fireRate) return false;
        var ammo = weapon.GetAmmoModule();
        if (ammo != null && ammo.GetCurrentAmmo() <= 0) return false;
        return readyToShoot && !isFiring;
    }

    public IEnumerator Fire()
    {
        if (!CanFire()) yield break;

        isFiring = true;
        readyToShoot = false;
        lastShotTime = Time.time;

        var proj = weapon.GetProjectileModule();
        var ammo = weapon.GetAmmoModule();

        if (ammo != null && ammo.GetCurrentAmmo() <= 0)
        {
            Debug.Log("Out of ammo!");
            isFiring = false;
            readyToShoot = true;
            yield break;
        }

        if (proj != null)
        {
            // Compute base direction
            Vector3 baseDir = weapon.CalculateBaseDirection();

            // Generate spread pattern
            Vector3[] pelletDirections = GenerateSpreadPattern(baseDir, pelletCount);

            // Fire each pellet
            for (int i = 0; i < pelletDirections.Length; i++)
            {
                GameObject b = proj.CreateProjectile(
                    weapon.FirePoint.position,
                    pelletDirections[i],
                    100f
                );

                if (b != null && bulletLifetimeOverride > 0f)
                {
                    Destroy(b, bulletLifetimeOverride);
                }
            }

            if (ammo != null) ammo.ConsumeAmmo(1);
        }

        yield return new WaitForSeconds(fireRate);
        readyToShoot = true;
        isFiring = false;
    }

    private Vector3[] GenerateSpreadPattern(Vector3 baseDirection, int count)
    {
        Vector3[] directions = new Vector3[count];

        switch (spreadPattern)
        {
            case SpreadPattern.Circular:
                if (useGoldenRatio)
                    GenerateGoldenSpiralPattern(baseDirection, directions);
                else
                    GenerateEvenCircularPattern(baseDirection, directions);
                break;

            case SpreadPattern.RandomCone:
                GenerateRandomConePattern(baseDirection, directions);
                break;

            case SpreadPattern.Ring:
                GenerateRingPattern(baseDirection, directions);
                break;

            case SpreadPattern.Cross:
                GenerateCrossPattern(baseDirection, directions);
                break;

            case SpreadPattern.Star:
                GenerateStarPattern(baseDirection, directions);
                break;
        }

        return directions;
    }

    private void GenerateGoldenSpiralPattern(Vector3 baseDir, Vector3[] directions)
    {
        // Use golden angle spiral for optimal distribution
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, baseDir).normalized;
        if (right.magnitude < 0.1f) // Handle edge case when looking straight up/down
        {
            right = Vector3.Cross(Vector3.forward, baseDir).normalized;
        }
        up = Vector3.Cross(baseDir, right).normalized;

        for (int i = 0; i < directions.Length; i++)
        {
            float t = i / (float)(directions.Length - 1);
            float radius = t * spreadAngle;
            float angle = i * GOLDEN_ANGLE;

            // Convert to radians
            float angleRad = angle * Mathf.Deg2Rad;
            float radiusRad = radius * Mathf.Deg2Rad;

            // Calculate offset
            Vector3 offset = (right * Mathf.Cos(angleRad) + up * Mathf.Sin(angleRad)) * Mathf.Sin(radiusRad);
            directions[i] = (baseDir * Mathf.Cos(radiusRad) + offset).normalized;
        }
    }

    private void GenerateEvenCircularPattern(Vector3 baseDir, Vector3[] directions)
    {
        // Traditional even circular pattern
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, baseDir).normalized;
        if (right.magnitude < 0.1f)
        {
            right = Vector3.Cross(Vector3.forward, baseDir).normalized;
        }
        up = Vector3.Cross(baseDir, right).normalized;

        // Place pellets in concentric circles
        int ringsCount = Mathf.CeilToInt(Mathf.Sqrt(directions.Length));
        int pelletIndex = 0;

        for (int ring = 0; ring < ringsCount && pelletIndex < directions.Length; ring++)
        {
            float ringRadius = (ring + 1) / (float)ringsCount * spreadAngle;
            int pelletsInRing = Mathf.Min(directions.Length - pelletIndex, ring == 0 ? 1 : ring * 6);

            for (int i = 0; i < pelletsInRing && pelletIndex < directions.Length; i++)
            {
                float angle = i * 360f / pelletsInRing;
                float angleRad = angle * Mathf.Deg2Rad;
                float radiusRad = ringRadius * Mathf.Deg2Rad;

                Vector3 offset = (right * Mathf.Cos(angleRad) + up * Mathf.Sin(angleRad)) * Mathf.Sin(radiusRad);
                directions[pelletIndex] = (baseDir * Mathf.Cos(radiusRad) + offset).normalized;
                pelletIndex++;
            }
        }
    }

    private void GenerateRandomConePattern(Vector3 baseDir, Vector3[] directions)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            // Random angle within spread
            float randomAngle = Random.Range(0f, spreadAngle);
            float randomRotation = Random.Range(0f, 360f);

            Quaternion spread = Quaternion.AngleAxis(randomAngle, Vector3.right);
            Quaternion rotation = Quaternion.AngleAxis(randomRotation, Vector3.forward);

            directions[i] = Quaternion.LookRotation(baseDir) * rotation * spread * Vector3.forward;
        }
    }

    private void GenerateRingPattern(Vector3 baseDir, Vector3[] directions)
    {
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, baseDir).normalized;
        if (right.magnitude < 0.1f)
        {
            right = Vector3.Cross(Vector3.forward, baseDir).normalized;
        }
        up = Vector3.Cross(baseDir, right).normalized;

        for (int i = 0; i < directions.Length; i++)
        {
            float angle = i * 360f / directions.Length;
            float angleRad = angle * Mathf.Deg2Rad;
            float radiusRad = spreadAngle * Mathf.Deg2Rad;

            Vector3 offset = (right * Mathf.Cos(angleRad) + up * Mathf.Sin(angleRad)) * Mathf.Sin(radiusRad);
            directions[i] = (baseDir * Mathf.Cos(radiusRad) + offset).normalized;
        }
    }

    private void GenerateCrossPattern(Vector3 baseDir, Vector3[] directions)
    {
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, baseDir).normalized;
        if (right.magnitude < 0.1f)
        {
            right = Vector3.Cross(Vector3.forward, baseDir).normalized;
        }
        up = Vector3.Cross(baseDir, right).normalized;

        int pelletsPerArm = directions.Length / 4;
        int pelletIndex = 0;

        // Four arms of the cross
        Vector3[] armDirections = { right, -right, up, -up };

        foreach (var armDir in armDirections)
        {
            for (int i = 0; i < pelletsPerArm && pelletIndex < directions.Length; i++)
            {
                float t = (i + 1) / (float)pelletsPerArm;
                float angle = t * spreadAngle * Mathf.Deg2Rad;

                directions[pelletIndex] = (baseDir * Mathf.Cos(angle) + armDir * Mathf.Sin(angle)).normalized;
                pelletIndex++;
            }
        }

        // Center pellet if odd number
        if (pelletIndex < directions.Length)
        {
            directions[pelletIndex] = baseDir;
        }
    }

    private void GenerateStarPattern(Vector3 baseDir, Vector3[] directions)
    {
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, baseDir).normalized;
        if (right.magnitude < 0.1f)
        {
            right = Vector3.Cross(Vector3.forward, baseDir).normalized;
        }
        up = Vector3.Cross(baseDir, right).normalized;

        int arms = 5; // 5-pointed star
        int pelletsPerArm = directions.Length / arms;
        int pelletIndex = 0;

        for (int arm = 0; arm < arms; arm++)
        {
            float armAngle = arm * 360f / arms;
            float armAngleRad = armAngle * Mathf.Deg2Rad;
            Vector3 armDirection = right * Mathf.Cos(armAngleRad) + up * Mathf.Sin(armAngleRad);

            for (int i = 0; i < pelletsPerArm && pelletIndex < directions.Length; i++)
            {
                float t = (i + 1) / (float)pelletsPerArm;
                float spreadRad = t * spreadAngle * Mathf.Deg2Rad;

                directions[pelletIndex] = (baseDir * Mathf.Cos(spreadRad) + armDirection * Mathf.Sin(spreadRad)).normalized;
                pelletIndex++;
            }
        }
    }

    // Preset configurations
    [ContextMenu("Preset: Pump Shotgun")]
    public void PresetPumpShotgun()
    {
        pelletCount = 8;
        spreadAngle = 12f;
        spreadPattern = SpreadPattern.Circular;
        useGoldenRatio = true;
        fireRate = 0.8f;
    }

    [ContextMenu("Preset: Combat Shotgun")]
    public void PresetCombatShotgun()
    {
        pelletCount = 12;
        spreadAngle = 15f;
        spreadPattern = SpreadPattern.Circular;
        useGoldenRatio = true;
        fireRate = 0.5f;
    }

    [ContextMenu("Preset: Sawed-Off")]
    public void PresetSawedOff()
    {
        pelletCount = 16;
        spreadAngle = 25f;
        spreadPattern = SpreadPattern.RandomCone;
        fireRate = 0.3f;
    }
}
// end