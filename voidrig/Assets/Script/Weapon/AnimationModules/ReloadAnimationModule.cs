// ReloadAnimationModule.cs
using System.Collections;
using UnityEngine;

public class ReloadAnimationModule : MonoBehaviour, IWeaponModule
{
    [Header("Animation Settings")]
    public float reloadLiftAngle = 45f;
    public float animationSpeed = 2f;
    public AnimationCurve reloadCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Transform Settings")]
    public Transform weaponPivot;
    public bool autoFindWeaponModel = true;

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
            Transform[] children = weapon.GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                if (child.name.ToLower().Contains("model") || child.name.ToLower().Contains("gun"))
                {
                    weaponPivot = child;
                    break;
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
        if (currentReloadAnimation != null)
        {
            StopCoroutine(currentReloadAnimation);
            currentReloadAnimation = null;
        }

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

        if (currentReloadAnimation != null)
        {
            StopCoroutine(currentReloadAnimation);
        }

        currentReloadAnimation = StartCoroutine(PlayReloadAnimation());
    }

    private IEnumerator PlayReloadAnimation()
    {
        isAnimating = true;

        float reloadDuration = 2f; // Default reload time
        var ammoModule = weapon.GetAmmoModule() as StandardAmmoModule;
        if (ammoModule != null)
        {
            reloadDuration = ammoModule.reloadTime;
        }

        float halfDuration = reloadDuration * 0.5f;

        Vector3 startRotation = originalRotation;
        Vector3 liftRotation = originalRotation + new Vector3(-reloadLiftAngle, 0, 0);

        // Phase 1: Lift the gun
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

        weaponPivot.localEulerAngles = liftRotation;

        // Phase 2: Lower the gun back
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

        weaponPivot.localEulerAngles = originalRotation;

        isAnimating = false;
        currentReloadAnimation = null;
    }

    public bool IsAnimating() => isAnimating;
}
// end