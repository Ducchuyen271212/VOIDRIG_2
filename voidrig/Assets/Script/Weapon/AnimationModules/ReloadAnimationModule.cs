// ReloadAnimationModule.cs
using System.Collections;
using UnityEngine;

public class ReloadAnimationModule : MonoBehaviour, IWeaponModule
{
    [Header("Animation Settings")]
    public float reloadLiftAngle = 45f; // How much to lift the back of the gun (degrees)
    public float animationSpeed = 2f; // How fast the animation plays
    public AnimationCurve reloadCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Animation curve for smooth motion

    [Header("Transform Settings")]
    public Transform weaponPivot; // The transform to rotate (usually the weapon model)
    public bool autoFindWeaponModel = true; // Auto-find weapon model if pivot not set

    private ModularWeapon weapon;
    private Vector3 originalRotation;
    private bool isAnimating = false;
    private Coroutine currentReloadAnimation;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;

        // Auto-find weapon model if not set
        if (autoFindWeaponModel && weaponPivot == null)
        {
            weaponPivot = weapon.weaponModel;
            if (weaponPivot == null)
            {
                // Try to find any child with "model" in the name
                Transform[] children = weapon.GetComponentsInChildren<Transform>();
                foreach (var child in children)
                {
                    if (child.name.ToLower().Contains("model") || child.name.ToLower().Contains("gun"))
                    {
                        weaponPivot = child;
                        break;
                    }
                }
            }

            // Fallback to weapon transform itself
            if (weaponPivot == null)
            {
                weaponPivot = weapon.transform;
            }
        }

        if (weaponPivot != null)
        {
            originalRotation = weaponPivot.localEulerAngles;
            Debug.Log($"ReloadAnimationModule initialized with pivot: {weaponPivot.name}");
        }
        else
        {
            Debug.LogError("ReloadAnimationModule: No weapon pivot found!");
        }
    }

    public void OnWeaponActivated()
    {
        if (weaponPivot != null)
        {
            originalRotation = weaponPivot.localEulerAngles;
        }
    }

    public void OnWeaponDeactivated()
    {
        // Stop any ongoing animation
        if (currentReloadAnimation != null)
        {
            StopCoroutine(currentReloadAnimation);
            currentReloadAnimation = null;
        }

        // Reset to original position
        if (weaponPivot != null)
        {
            weaponPivot.localEulerAngles = originalRotation;
        }

        isAnimating = false;
    }

    public void OnUpdate()
    {
        // Listen for reload events from ammo module
        var ammoModule = weapon.GetAmmoModule() as StandardAmmoModule;
        if (ammoModule != null && ammoModule.IsReloading() && !isAnimating)
        {
            StartReloadAnimation();
        }
    }

    public void StartReloadAnimation()
    {
        if (weaponPivot == null || isAnimating) return;

        Debug.Log("Starting reload animation");

        if (currentReloadAnimation != null)
        {
            StopCoroutine(currentReloadAnimation);
        }

        currentReloadAnimation = StartCoroutine(PlayReloadAnimation());
    }

    private IEnumerator PlayReloadAnimation()
    {
        isAnimating = true;

        float reloadDuration = weapon.WeaponData?.reloadTime ?? 2f;
        float halfDuration = reloadDuration * 0.5f;

        Vector3 startRotation = originalRotation;
        Vector3 liftRotation = originalRotation + new Vector3(-reloadLiftAngle, 0, 0); // Lift back of gun

        // Phase 1: Lift the gun (first half of reload)
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            float curveValue = reloadCurve.Evaluate(progress);

            Vector3 currentRotation = Vector3.Lerp(startRotation, liftRotation, curveValue);
            weaponPivot.localEulerAngles = currentRotation;

            yield return null;
        }

        // Ensure we're at the exact lift position
        weaponPivot.localEulerAngles = liftRotation;

        // Phase 2: Lower the gun back (second half of reload)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            float curveValue = reloadCurve.Evaluate(progress);

            Vector3 currentRotation = Vector3.Lerp(liftRotation, startRotation, curveValue);
            weaponPivot.localEulerAngles = currentRotation;

            yield return null;
        }

        // Ensure we're back to original position
        weaponPivot.localEulerAngles = originalRotation;

        isAnimating = false;
        currentReloadAnimation = null;

        Debug.Log("Reload animation completed");
    }

    // Manual trigger for reload animation (can be called from other scripts)
    public void TriggerReloadAnimation()
    {
        StartReloadAnimation();
    }

    // Check if currently animating
    public bool IsAnimating() => isAnimating;

    private void OnValidate()
    {
        // Clamp values in inspector
        reloadLiftAngle = Mathf.Clamp(reloadLiftAngle, 0f, 90f);
        animationSpeed = Mathf.Max(0.1f, animationSpeed);
    }
}
// end