//ScopeAbility.cs
using UnityEngine;
using System.Collections;

public class ScopeAbility : BaseAbilityModule
{
    [Header("Scope Settings")]
    [Tooltip("Higher values = more zoom (0 = no zoom, 50 = high zoom)")]
    public float scopeZoomLevel = 30f;
    public float accuracyBonus = 0.2f;

    [Header("Scope Positioning")]
    [Tooltip("Position offset when using scope")]
    public Vector3 scopePositionOffset = new Vector3(0, 0.05f, 0.25f);

    [Tooltip("Rotation offset when using scope")]
    public Vector3 scopeRotationOffset = Vector3.zero;

    [Header("Transition Settings")]
    [Tooltip("Time to transition in/out of scope")]
    public float scopeTransitionTime = 0.3f;
    public AnimationCurve scopeTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Scope Visual")]
    public GameObject scopeOverlay;
    public bool showScopeOverlay = true;

    [Header("Scope Behavior")]
    [Tooltip("Should scope activate automatically when aiming?")]
    public bool autoActivateOnAim = true;

    private Camera playerCamera;
    private float originalFOV = 60f;
    private bool isScoped = false;
    private bool isTransitioning = false;

    // Store original weapon transform
    private Vector3 originalWeaponPosition;
    private Vector3 originalWeaponRotation;
    private bool hasStoredOriginalTransform = false;

    // Smooth transition coroutine
    private Coroutine transitionCoroutine;

    public override void Initialize(ModularWeapon weapon)
    {
        base.Initialize(weapon);
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            originalFOV = playerCamera.fieldOfView;
        }

        // IMPORTANT: Set cooldown to 0 to prevent delay
        if (abilityData != null)
        {
            abilityData.cooldown = 0f;
        }

        Debug.Log($"ScopeAbility initialized - Zoom Level: {scopeZoomLevel} (higher = more zoom)");
    }

    protected override void OnAbilityActivated()
    {
        if (isScoped || isTransitioning) return;

        // Store original transform if not already stored
        if (!hasStoredOriginalTransform && weapon != null)
        {
            originalWeaponPosition = weapon.transform.localPosition;
            originalWeaponRotation = weapon.transform.localEulerAngles;
            hasStoredOriginalTransform = true;
        }

        // Start smooth transition to scope
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(SmoothScopeTransition(true));

        weapon.SetAnimationTrigger("ScopeUp");

        Debug.Log($"Scope activation started - Target zoom: {scopeZoomLevel}");
    }

    protected override void OnAbilityDeactivated()
    {
        if (!isScoped && !isTransitioning) return;

        // Start smooth transition out of scope
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(SmoothScopeTransition(false));

        weapon.SetAnimationTrigger("ScopeDown");

        Debug.Log("Scope deactivation started");
    }

    private IEnumerator SmoothScopeTransition(bool scopeIn)
    {
        isTransitioning = true;
        float elapsedTime = 0f;

        // Calculate target FOV using inverted system
        float targetFOV = scopeIn ? CalculateTargetFOV() : originalFOV;
        float startFOV = playerCamera.fieldOfView;

        // Calculate target weapon transform
        Vector3 targetPosition = scopeIn ? originalWeaponPosition + scopePositionOffset : originalWeaponPosition;
        Vector3 targetRotation = scopeIn ? originalWeaponRotation + scopeRotationOffset : originalWeaponRotation;
        Vector3 startPosition = weapon.transform.localPosition;
        Vector3 startRotation = weapon.transform.localEulerAngles;

        // Show/hide scope overlay at start of transition
        if (scopeIn && showScopeOverlay && scopeOverlay != null)
        {
            scopeOverlay.SetActive(true);
        }

        // Smooth transition
        while (elapsedTime < scopeTransitionTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / scopeTransitionTime;
            float curvedProgress = scopeTransitionCurve.Evaluate(progress);

            // Smooth FOV transition
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, curvedProgress);
            }

            // Smooth weapon position transition
            if (weapon != null)
            {
                weapon.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, curvedProgress);
                weapon.transform.localEulerAngles = Vector3.Lerp(startRotation, targetRotation, curvedProgress);
            }

            yield return null;
        }

        // Ensure final values are set
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = targetFOV;
        }

        if (weapon != null)
        {
            weapon.transform.localPosition = targetPosition;
            weapon.transform.localEulerAngles = targetRotation;
        }

        // Hide scope overlay at end of scope-out transition
        if (!scopeIn && scopeOverlay != null)
        {
            scopeOverlay.SetActive(false);
        }

        isScoped = scopeIn;
        isTransitioning = false;

        Debug.Log($"Scope transition complete - Scoped: {isScoped}, Final FOV: {playerCamera.fieldOfView}");
    }

    private float CalculateTargetFOV()
    {
        // INVERTED SYSTEM: Higher scopeZoomLevel = more zoom = lower FOV
        // Convert zoom level to FOV: 0 zoom = original FOV, higher zoom = lower FOV

        if (scopeZoomLevel <= 0)
        {
            return originalFOV; // No zoom
        }

        // Formula: Higher zoom level = lower FOV
        // Example: zoom 30 = FOV 30, zoom 50 = FOV 10
        float targetFOV = originalFOV - scopeZoomLevel;

        // Clamp to reasonable values (minimum FOV of 5 degrees)
        return Mathf.Clamp(targetFOV, 5f, originalFOV);
    }

    public override void OnUpdate()
    {
        // ONLY work if this component is enabled
        if (!enabled) return;

        base.OnUpdate();

        // Only auto-activate if enabled and not transitioning
        if (autoActivateOnAim && AimingManager.Instance != null && !isTransitioning)
        {
            bool shouldBeScoped = AimingManager.Instance.isAiming;

            if (shouldBeScoped && !isScoped && CanActivate())
            {
                ActivateAbility();
            }
            else if (!shouldBeScoped && isScoped)
            {
                DeactivateAbility();
            }
        }
    }

    public override void OnWeaponActivated()
    {
        base.OnWeaponActivated();

        // Store the weapon's original transform when first activated
        if (weapon != null && !hasStoredOriginalTransform)
        {
            originalWeaponPosition = weapon.transform.localPosition;
            originalWeaponRotation = weapon.transform.localEulerAngles;
            hasStoredOriginalTransform = true;
            Debug.Log($"Stored original weapon transform: {originalWeaponPosition}");
        }
    }

    public override void OnWeaponDeactivated()
    {
        base.OnWeaponDeactivated();

        // Stop any ongoing transitions
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        // Force immediate scope deactivation
        if (isScoped || isTransitioning)
        {
            isScoped = false;
            isTransitioning = false;

            // Restore original FOV immediately
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = originalFOV;
            }

            // Hide scope overlay
            if (scopeOverlay != null)
            {
                scopeOverlay.SetActive(false);
            }
        }

        // Reset stored transform flag
        hasStoredOriginalTransform = false;
    }

    public override bool CanActivate()
    {
        // FIXED: Remove cooldown check to prevent delay, but check if not transitioning
        return !isScoped && !isTransitioning && enabled;
    }

    // Public getters
    public bool IsScoped() => isScoped;
    public bool IsTransitioning() => isTransitioning;
    public Vector3 GetScopePositionOffset() => scopePositionOffset;
    public Vector3 GetScopeRotationOffset() => scopeRotationOffset;

    // Manual activation methods
    [ContextMenu("Activate Scope")]
    public void ManualActivateScope()
    {
        if (CanActivate())
        {
            ActivateAbility();
        }
    }

    [ContextMenu("Deactivate Scope")]
    public void ManualDeactivateScope()
    {
        if (isScoped || isTransitioning)
        {
            DeactivateAbility();
        }
    }

    // Preset configurations for different weapon types (using NEW zoom system)
    [ContextMenu("Preset: Sniper Scope")]
    public void PresetSniperScope()
    {
        scopeZoomLevel = 45f; // High zoom (was 15f in old system)
        scopePositionOffset = new Vector3(0, 0.08f, 0.3f);
        scopeRotationOffset = Vector3.zero;
        scopeTransitionTime = 0.5f; // Slower transition for sniper
        accuracyBonus = 0.1f;
        showScopeOverlay = true;
        autoActivateOnAim = true;
    }

    [ContextMenu("Preset: ACOG Scope")]
    public void PresetACOGScope()
    {
        scopeZoomLevel = 25f; // Medium zoom (was 30f in old system)
        scopePositionOffset = new Vector3(0, 0.05f, 0.2f);
        scopeRotationOffset = Vector3.zero;
        scopeTransitionTime = 0.3f; // Standard transition
        accuracyBonus = 0.3f;
        showScopeOverlay = true;
        autoActivateOnAim = true;
    }

    [ContextMenu("Preset: Red Dot Sight")]
    public void PresetRedDotSight()
    {
        scopeZoomLevel = 5f; // Low zoom (was 50f in old system)
        scopePositionOffset = new Vector3(0, 0.03f, 0.15f);
        scopeRotationOffset = Vector3.zero;
        scopeTransitionTime = 0.2f; // Fast transition
        accuracyBonus = 0.4f;
        showScopeOverlay = false; // No overlay for red dot
        autoActivateOnAim = true;
    }
}
//end