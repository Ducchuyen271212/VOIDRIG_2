// PhysicalProjectileModule.cs
using UnityEngine;

public class SimplifiedPhysicalProjectileModule : MonoBehaviour, IProjectileModule
{
    [Header("Simple Projectile Settings")]
    public GameObject bulletPrefab; // Your existing bullet prefab

    [Header("Bullet Behavior")]
    public float lifetimeOverride = -1f; // -1 = use weapon data, otherwise override

    [Header("Bullet Trail Settings")]
    public bool enableTrails = true;
    public Material trailMaterial;
    public Color trailColor = Color.red;
    public float trailWidth = 0.1f;
    public float trailLength = 0.5f; // How long the trail lasts
    public AnimationCurve trailWidthCurve = AnimationCurve.Linear(0, 1, 1, 0.1f); // Width over lifetime

    [Header("Trail Shape")]
    public int cornerVertices = 2;
    public int capVertices = 2;
    public float minVertexDistance = 0.01f;
    public bool autodestruct = true;

    [Header("Trail Advanced")]
    public Gradient trailColorGradient; // Optional gradient override
    public bool useGradient = false;

    private ModularWeapon weapon;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;

        // Try to auto-find bullet prefab if not assigned
        if (bulletPrefab == null)
        {
            var oldWeapon = weapon.GetComponent<Weapon>();
            if (oldWeapon != null && oldWeapon.bulletPrefab != null)
            {
                bulletPrefab = oldWeapon.bulletPrefab;
                Debug.Log($"Auto-found bullet prefab: {bulletPrefab.name}");
            }
        }

        // Create default trail material if trails are enabled but no material assigned
        if (enableTrails && trailMaterial == null)
        {
            CreateDefaultTrailMaterial();
        }

        // Initialize default gradient if not set
        if (trailColorGradient == null)
        {
            trailColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(trailColor, 0.0f);
            colorKeys[1] = new GradientColorKey(trailColor, 1.0f);
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(0.0f, 1.0f);
            trailColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }

    public void OnWeaponActivated() { }
    public void OnWeaponDeactivated() { }
    public void OnUpdate() { }

    public GameObject CreateProjectile(Vector3 position, Vector3 direction, float velocity)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("No bullet prefab assigned to SimplifiedPhysicalProjectileModule!");
            return null;
        }

        // Create the bullet
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));

        // Set damage using existing Bullet script
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null && weapon.WeaponData != null)
        {
            bulletScript.damage = weapon.WeaponData.damage;
        }

        // Apply velocity
        var rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * velocity;
        }

        // Add trail if enabled
        if (enableTrails)
        {
            AddTrailToProjectile(bullet);
        }

        // Handle lifetime
        float lifetime = lifetimeOverride > 0 ? lifetimeOverride : (weapon.WeaponData?.bulletLifeTime ?? 5f);
        Destroy(bullet, lifetime);

        Debug.Log($"Created bullet with lifetime: {lifetime}s, trails: {enableTrails}");

        return bullet;
    }

    private void AddTrailToProjectile(GameObject projectile)
    {
        if (projectile == null) return;

        TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = projectile.AddComponent<TrailRenderer>();
        }

        // Basic trail settings
        trail.material = trailMaterial;
        trail.time = trailLength;
        trail.minVertexDistance = minVertexDistance;
        trail.autodestruct = autodestruct;

        // Shape settings
        trail.numCornerVertices = cornerVertices;
        trail.numCapVertices = capVertices;

        // Width settings - use simple start/end width for compatibility
        trail.startWidth = trailWidth;
        trail.endWidth = trailWidth * 0.1f; // Taper to thin end

        // Try to use width curve if supported
        try
        {
            trail.widthCurve = trailWidthCurve;

            // Scale the width curve by our trail width
            AnimationCurve scaledCurve = new AnimationCurve();
            for (int i = 0; i < trailWidthCurve.keys.Length; i++)
            {
                Keyframe key = trailWidthCurve.keys[i];
                key.value *= trailWidth;
                scaledCurve.AddKey(key);
            }
            trail.widthCurve = scaledCurve;
        }
        catch
        {
            // Fallback to simple width if widthCurve not available
            Debug.Log("Using simple trail width (widthCurve not available)");
        }

        // Color settings - use simple color for compatibility
        if (useGradient && trailColorGradient != null)
        {
            try
            {
                trail.colorGradient = trailColorGradient;
            }
            catch
            {
                // Fallback to simple color
                trail.startColor = trailColor;
                trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f); // Fade out
                Debug.Log("Using simple trail color (colorGradient not available)");
            }
        }
        else
        {
            // Use simple start/end color
            trail.startColor = trailColor;
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f); // Fade out
        }

        // Make sure trail renders properly
        trail.sortingOrder = 1;

        Debug.Log($"Added trail to projectile: {projectile.name} - Color: {trailColor}, Width: {trailWidth}, Length: {trailLength}");
    }

    private void CreateDefaultTrailMaterial()
    {
        // Try to use Unlit/Color shader first, fallback to Sprites/Default
        Shader trailShader = Shader.Find("Unlit/Color");
        if (trailShader == null)
        {
            trailShader = Shader.Find("Sprites/Default");
        }

        trailMaterial = new Material(trailShader);
        trailMaterial.color = trailColor;
        trailMaterial.name = "DefaultBulletTrailMaterial";

        Debug.Log("Created default trail material");
    }

    // Public methods to change trail settings at runtime
    public void SetTrailColor(Color color)
    {
        trailColor = color;
        if (trailMaterial != null)
        {
            trailMaterial.color = color;
        }
    }

    public void SetTrailWidth(float width)
    {
        trailWidth = width;
    }

    public void SetTrailLength(float length)
    {
        trailLength = length;
    }

    public void ToggleTrails(bool enabled)
    {
        enableTrails = enabled;
    }

    // Preset trail configurations
    [ContextMenu("Preset: Thin Laser")]
    public void PresetThinLaser()
    {
        enableTrails = true;
        trailColor = Color.red;
        trailWidth = 0.05f;
        trailLength = 0.3f;
        trailWidthCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);
        useGradient = false;
    }

    [ContextMenu("Preset: Thick Bullet")]
    public void PresetThickBullet()
    {
        enableTrails = true;
        trailColor = Color.yellow;
        trailWidth = 0.2f;
        trailLength = 0.7f;
        trailWidthCurve = AnimationCurve.Linear(0, 1, 1, 0.3f);
        useGradient = false;
    }

    [ContextMenu("Preset: Fading Trail")]
    public void PresetFadingTrail()
    {
        enableTrails = true;
        trailColor = Color.white;
        trailWidth = 0.15f;
        trailLength = 1.0f;

        // Create a curve that starts thick and fades
        trailWidthCurve = new AnimationCurve();
        trailWidthCurve.AddKey(0f, 1f);
        trailWidthCurve.AddKey(0.5f, 0.7f);
        trailWidthCurve.AddKey(1f, 0f);

        useGradient = true;

        // Create gradient that fades from white to transparent (if supported)
        try
        {
            trailColorGradient = new Gradient();
            var colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.white, 0.0f);
            colorKeys[1] = new GradientColorKey(Color.cyan, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.blue, 1.0f);
            var alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(0.0f, 1.0f);
            trailColorGradient.SetKeys(colorKeys, alphaKeys);
        }
        catch
        {
            // Fallback to simple white color
            trailColor = Color.white;
            useGradient = false;
        }
    }

    public ProjectileType GetProjectileType() => ProjectileType.Physical;

    private void OnValidate()
    {
        // Clamp values in inspector
        trailWidth = Mathf.Max(0.01f, trailWidth);
        trailLength = Mathf.Max(0.1f, trailLength);
        minVertexDistance = Mathf.Max(0.001f, minVertexDistance);
        cornerVertices = Mathf.Clamp(cornerVertices, 0, 10);
        capVertices = Mathf.Clamp(capVertices, 0, 10);
    }
}
// end