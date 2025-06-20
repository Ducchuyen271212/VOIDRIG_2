// ModularWeapon.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ModularWeapon : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public Transform firePoint;
    public Transform weaponModel;
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    [Header("=== STATE ===")]
    private bool _isActiveWeapon = false;
    public bool isActiveWeapon
    {
        get => _isActiveWeapon;
        set
        {
            if (_isActiveWeapon != value)
            {
                _isActiveWeapon = value;

                if (_isActiveWeapon && transform.parent != null)
                {
                    Debug.Log("Weapon activated - setting up input");
                    SetupInputActions();
                }
                else if (!_isActiveWeapon)
                {
                    Debug.Log("Weapon deactivated - disabling input");
                    DisableInputActions();
                }
            }
        }
    }

    // === MODULE REFERENCES ===
    private List<IWeaponModule> allModules = new List<IWeaponModule>();
    private IFireModule fireModule; // Single fire module - let the module handle its own modes
    private IProjectileModule projectileModule;
    private IAmmoModule ammoModule;
    private ITargetingModule targetingModule;
    private List<IAbilityModule> abilityModules = new List<IAbilityModule>();

    // === COMPONENTS ===
    private Animator animator;
    private AudioSource audioSource;
    private PlayerInput playerInput;

    // === INPUT ===
    private InputAction attackAction;
    private InputAction reloadAction;
    private InputAction switchModeAction;
    private InputAction dropAction;

    // === PROPERTIES ===
    public Transform FirePoint => firePoint;
    public Animator WeaponAnimator => animator;
    public AudioSource WeaponAudio => audioSource;
    public PlayerInput PlayerInputRef => playerInput;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        DiscoverModules();
    }

    private void OnEnable()
    {
        if (transform.parent != null)
        {
            SetupInputActions();
        }
    }

    private void OnDisable()
    {
        isActiveWeapon = false;
        NotifyModules(module => module.OnWeaponDeactivated());
        DisableInputActions();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("=== WEAPON DEBUG ===");
            Debug.Log($"isActiveWeapon: {isActiveWeapon}");
            Debug.Log($"Parent: {transform.parent?.name ?? "NO PARENT"}");
            Debug.Log($"PlayerInput found: {playerInput != null}");

            // Input Actions Status
            Debug.Log($"Attack action: {attackAction?.name ?? "NULL"} | Enabled: {attackAction?.enabled ?? false}");
            Debug.Log($"Reload action: {reloadAction?.name ?? "NULL"} | Enabled: {reloadAction?.enabled ?? false}");
            Debug.Log($"SwitchMode action: {switchModeAction?.name ?? "NULL"} | Enabled: {switchModeAction?.enabled ?? false}");
            Debug.Log($"DropItem action: {dropAction?.name ?? "NULL"} | Enabled: {dropAction?.enabled ?? false}");

            // Module Status
            Debug.Log($"Fire Module: {fireModule?.GetType().Name ?? "NULL"}");
            Debug.Log($"Projectile Module: {projectileModule?.GetType().Name ?? "NULL"}");
            Debug.Log($"Ammo Module: {ammoModule?.GetType().Name ?? "NULL"}");
            Debug.Log($"Can Reload: {ammoModule?.CanReload() ?? false}");
            Debug.Log($"Current Ammo: {ammoModule?.GetCurrentAmmo() ?? -1}");
            Debug.Log($"Total Modules: {allModules.Count}");

            // Collider Status
            var collider = GetComponent<Collider>();
            Debug.Log($"Collider enabled: {collider?.enabled ?? false}");
        }

        if (!isActiveWeapon) return;

        HandleInput();
        NotifyModules(module => module.OnUpdate());
        UpdateUI();
    }

    // === MODULE DISCOVERY ===
    private void DiscoverModules()
    {
        allModules.Clear();
        var modules = GetComponentsInChildren<IWeaponModule>();

        foreach (var module in modules)
        {
            RegisterModule(module);
        }

        Debug.Log($"Discovered {allModules.Count} modules");
    }

    public void RegisterModule(IWeaponModule module)
    {
        if (allModules.Contains(module)) return;

        allModules.Add(module);

        // Register specific module types
        if (module is IFireModule fire) fireModule = fire;
        if (module is IProjectileModule projectile) projectileModule = projectile;
        if (module is IAmmoModule ammo) ammoModule = ammo;
        if (module is ITargetingModule targeting) targetingModule = targeting;
        if (module is IAbilityModule ability) abilityModules.Add(ability);

        module.Initialize(this);
    }

    // === INITIALIZATION ===
    private void InitializeModules()
    {
        NotifyModules(module => module.Initialize(this));
    }

    private void SetupInputActions()
    {
        // Try multiple ways to find PlayerInput
        playerInput = GetComponentInParent<PlayerInput>() ??
                     FindFirstObjectByType<PlayerInput>();

        if (playerInput == null && WeaponManager.Instance?.player != null)
        {
            playerInput = WeaponManager.Instance.player.GetComponent<PlayerInput>();
        }

        if (playerInput == null)
        {
            PlayerInput[] allInputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (allInputs.Length > 0) playerInput = allInputs[0];
        }

        Debug.Log($"PlayerInput search result: {playerInput != null}");

        if (playerInput?.actions != null)
        {
            try
            {
                attackAction = playerInput.actions["Attack"];
                attackAction?.Enable();
                Debug.Log($"Attack action found: {attackAction != null}");
            }
            catch { Debug.LogError("Attack action not found"); }

            try
            {
                reloadAction = playerInput.actions["Reload"];
                reloadAction?.Enable();
                Debug.Log($"Reload action found: {reloadAction != null}");
            }
            catch { Debug.LogError("Reload action not found"); }

            try
            {
                switchModeAction = playerInput.actions["SwitchMode"];
                switchModeAction?.Enable();
                Debug.Log($"SwitchMode action found: {switchModeAction != null}");
            }
            catch { Debug.LogError("SwitchMode action not found"); }

            try
            {
                dropAction = playerInput.actions["DropItem"];
                dropAction?.Enable();
                Debug.Log($"DropItem action found: {dropAction != null}");
            }
            catch { Debug.LogError("DropItem action not found"); }

            // Collider stays enabled always (always-on mode)
            Debug.Log("Weapon collider kept enabled (always-on mode)");

            isActiveWeapon = true;
            NotifyModules(module => module.OnWeaponActivated());

            Debug.Log("=== WEAPON ACTIVATED ===");
            Debug.Log($"All input actions enabled: Attack={attackAction?.enabled}, Reload={reloadAction?.enabled}");
        }
        else
        {
            Debug.LogError("PlayerInput or actions not found! Cannot activate weapon.");
        }
    }

    private void DisableInputActions()
    {
        attackAction?.Disable();
        reloadAction?.Disable();
        switchModeAction?.Disable();
        dropAction?.Disable();
    }

    // === INPUT HANDLING ===
    private void HandleInput()
    {
        // Fire module mode switching - let the fire module handle it
        if (switchModeAction?.WasPressedThisFrame() == true)
        {
            Debug.Log("SwitchMode pressed - delegating to fire module");
            // FlexibleFireModule will handle this in its OnUpdate
        }

        // Reload
        if (reloadAction?.WasPressedThisFrame() == true && ammoModule != null)
        {
            Debug.Log("Reload pressed");
            if (ammoModule.CanReload())
            {
                StartCoroutine(ammoModule.Reload());
            }
        }

        // Drop weapon
        if (dropAction?.WasPressedThisFrame() == true)
        {
            Debug.Log("Drop pressed");
            DropWeapon();
        }

        // Fire input - pass to fire module
        if (fireModule != null)
        {
            bool isPressed = attackAction?.IsPressed() ?? false;
            bool wasPressed = attackAction?.WasPressedThisFrame() ?? false;

            fireModule.OnFireInput(isPressed, wasPressed);
        }
    }

    // === UTILITY METHODS ===
    private void NotifyModules(System.Action<IWeaponModule> action)
    {
        foreach (var module in allModules)
        {
            try
            {
                if (module != null)
                {
                    action?.Invoke(module);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in module {module?.GetType().Name}: {e.Message}");
            }
        }
    }

    public Vector3 CalculateBaseDirection()
    {
        if (firePoint == null) firePoint = transform;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.GetPoint(100f);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            targetPoint = hit.point;

        return (targetPoint - firePoint.position).normalized;
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public void SetAnimationTrigger(string triggerName)
    {
        if (animator != null && HasAnimationParameter(triggerName))
            animator.SetTrigger(triggerName);
    }

    private bool HasAnimationParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (var param in animator.parameters)
            if (param.name == paramName) return true;

        return false;
    }

    private void UpdateUI()
    {
        if (AmmoManager.Instance?.ammoDisplay != null && ammoModule != null)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{ammoModule.GetCurrentAmmo()} / {ammoModule.GetTotalAmmo()}";
        }
    }

    private void DropWeapon()
    {
        Debug.Log("=== ULTIMATE DROP WEAPON FIX ===");

        // STEP 1: Force stop ALL aiming/scoping immediately
        if (AimingManager.Instance != null)
        {
            AimingManager.Instance.ForceStopAiming();
            Debug.Log("Force stopped aiming");
        }

        // STEP 2: Deactivate ALL abilities (especially scope)
        var abilities = GetAbilityModules();
        foreach (var ability in abilities)
        {
            if (ability is ScopeAbility scope && scope.IsScoped())
            {
                scope.ManualDeactivateScope();
                Debug.Log("Force deactivated scope ability");
            }
        }

        // STEP 3: Clear ammo UI immediately
        if (AmmoManager.Instance?.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.text = "-- / --";
            Debug.Log("Cleared ammo UI");
        }

        // STEP 4: Wait a frame to ensure all systems have processed the deactivation
        StartCoroutine(FinishDrop());
    }

    private System.Collections.IEnumerator FinishDrop()
    {
        yield return null; // Wait one frame

        Debug.Log("=== FINISHING DROP ===");

        // Deactivate weapon systems
        isActiveWeapon = false;
        NotifyModules(module => module.OnWeaponDeactivated());
        DisableInputActions();

        // Remove from parent
        Transform originalParent = transform.parent;
        transform.SetParent(null);
        Debug.Log($"Removed from parent: {originalParent?.name}");

        // Ensure collider is enabled
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log($"Collider enabled: {collider.enabled}");
        }

        // Reset and enable physics with full settings
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Reset all physics properties
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.linearVelocity = Vector3.zero; // Clear any existing velocity
            rb.angularVelocity = Vector3.zero; // Clear any existing rotation

            // Add drop force
            Vector3 dropForce = Camera.main.transform.forward * 3f + Vector3.up * 1f;
            rb.AddForce(dropForce, ForceMode.Impulse);

            Debug.Log($"Physics reset - isKinematic: {rb.isKinematic}, useGravity: {rb.useGravity}, velocity: {rb.linearVelocity}");
        }

        // Disable animator
        if (animator != null)
        {
            animator.enabled = false;
            Debug.Log("Animator disabled");
        }

        Debug.Log("=== WEAPON DROP COMPLETED ===");
    }

    // === GETTERS ===
    public T GetModule<T>() where T : class, IWeaponModule
    {
        foreach (var module in allModules)
            if (module is T typedModule) return typedModule;

        return fireModule as T;
    }

    public IFireModule GetFireModule() => fireModule;
    public IProjectileModule GetProjectileModule() => projectileModule;
    public IAmmoModule GetAmmoModule() => ammoModule;
    public ITargetingModule GetTargetingModule() => targetingModule;
    public List<IAbilityModule> GetAbilityModules() => abilityModules;

    // === INSPECTOR METHODS ===
    [ContextMenu("Refresh Modules")]
    public void EditorRefreshModules()
    {
        DiscoverModules();
    }

    private void OnValidate()
    {
        if (firePoint == null)
            firePoint = transform.Find("BulletSpawn") ?? transform;

        if (weaponModel == null)
            weaponModel = transform.Find("GunModel");
    }
}
// end