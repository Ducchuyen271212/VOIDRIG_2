//ModularWeapon.cs - Fixed Version
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ModularWeapon : MonoBehaviour
{
    [Header("Weapon Identity")]
    public string weaponName;
    public string weaponTag;

    [Header("Transform References")]
    public Transform firePoint;
    public Transform weaponModel;

    [Header("Positioning")]
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    [Header("State")]
    public bool isActiveWeapon = false; // Explicitly set to false by default
    private bool hasBeenPickedUpBefore = false;

    [Header("Core Data")]
    [SerializeField] private GameObject gunDataHolder;
    [SerializeField] private SoundData soundData;

    // Module References
    private List<IWeaponModule> allModules = new List<IWeaponModule>();
    private IFireModule fireModule;
    private IProjectileModule projectileModule;
    private IAmmoModule ammoModule;
    private ITargetingModule targetingModule;
    private List<IAbilityModule> abilityModules = new List<IAbilityModule>();

    // Core Components
    private Animator animator;
    private AudioSource audioSource;
    private PlayerInput playerInput;

    // Input Actions
    private InputAction attackAction;
    private InputAction reloadAction;
    private InputAction switchModeAction;
    private InputAction dropAction;
    private List<InputAction> abilityActions = new List<InputAction>(); // Changed to List

    // Weapon Data
    private GunData.Attribute weaponData;
    private SoundData.WeaponSound weaponSound;

    // Properties for modules to access
    public GunData.Attribute WeaponData => weaponData;
    public SoundData.WeaponSound WeaponSound => weaponSound;
    public Transform FirePoint => firePoint;
    public Animator WeaponAnimator => animator;
    public AudioSource WeaponAudio => audioSource;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // Auto-discover modules
        DiscoverModules();
    }

    private void Start()
    {
        // Ensure weapon starts as inactive
        isActiveWeapon = false;

        if (gunDataHolder != null)
        {
            InitializeWeaponData();
        }
    }

    private void OnEnable()
    {
        if (transform.parent != null)
        {
            SetupInputActions();
        }
        else
        {
            isActiveWeapon = false;
        }
    }

    private void OnDisable()
    {
        isActiveWeapon = false;
        NotifyModules(module => module.OnWeaponDeactivated());

        // Disable input actions
        DisableInputActions();
    }

    private void Update()
    {
        if (!isActiveWeapon || weaponData == null) return;

        HandleInput();
        NotifyModules(module => module.OnUpdate());
        UpdateUI();
    }

    // === MODULE MANAGEMENT ===

    private void DiscoverModules()
    {
        // Find all modules on this GameObject and children
        var modules = GetComponentsInChildren<IWeaponModule>();

        foreach (var module in modules)
        {
            RegisterModule(module);
        }
    }

    public void RegisterModule(IWeaponModule module)
    {
        if (allModules.Contains(module)) return;

        allModules.Add(module);

        // Type-specific registration
        if (module is IFireModule fire) fireModule = fire;
        if (module is IProjectileModule projectile) projectileModule = projectile;
        if (module is IAmmoModule ammo) ammoModule = ammo;
        if (module is ITargetingModule targeting) targetingModule = targeting;
        if (module is IAbilityModule ability) abilityModules.Add(ability);

        module.Initialize(this);
    }

    public void UnregisterModule(IWeaponModule module)
    {
        allModules.Remove(module);

        if (module == fireModule) fireModule = null;
        if (module == projectileModule) projectileModule = null;
        if (module == ammoModule) ammoModule = null;
        if (module == targetingModule) targetingModule = null;
        if (module is IAbilityModule ability) abilityModules.Remove(ability);
    }

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

    // === INITIALIZATION ===

    private void SetupInputActions()
    {
        // Try different ways to find PlayerInput
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }
        if (playerInput == null)
        {
            playerInput = GameObject.FindFirstObjectByType<PlayerInput>();
        }

        if (playerInput?.actions != null)
        {
            Debug.Log($"Found PlayerInput: {playerInput.name}");
            Debug.Log($"Available actions: {string.Join(", ", GetActionNames(playerInput.actions))}");

            // Setup basic actions with error handling
            try { attackAction = playerInput.actions["Attack"]; } catch { Debug.LogError("Attack action not found"); }
            try { reloadAction = playerInput.actions["Reload"]; } catch { Debug.LogError("Reload action not found"); }
            try { switchModeAction = playerInput.actions["SwitchMode"]; } catch { Debug.LogError("SwitchMode action not found"); }
            try { dropAction = playerInput.actions["DropItem"]; } catch { Debug.LogError("DropItem action not found"); }

            Debug.Log($"Input actions setup - Attack: {attackAction != null}, Reload: {reloadAction != null}, Drop: {dropAction != null}");

            // Setup ability actions
            abilityActions.Clear();
            for (int i = 0; i < abilityModules.Count && i < 4; i++)
            {
                string actionName = $"Ability{i + 1}";
                try
                {
                    var abilityAction = playerInput.actions[actionName];
                    if (abilityAction != null)
                    {
                        abilityActions.Add(abilityAction);
                        abilityAction.Enable();
                    }
                    else
                    {
                        abilityActions.Add(null); // Keep index consistency
                    }
                }
                catch
                {
                    abilityActions.Add(null); // Keep index consistency
                }
            }

            // Enable basic actions
            attackAction?.Enable();
            reloadAction?.Enable();
            switchModeAction?.Enable();
            dropAction?.Enable();

            isActiveWeapon = true;

            Debug.Log($"Modules found - Fire: {fireModule != null}, Projectile: {projectileModule != null}, Ammo: {ammoModule != null}");

            NotifyModules(module => module.OnWeaponActivated());
        }
        else
        {
            Debug.LogError($"PlayerInput not found! Searched in parent of {transform.name}");
        }
    }

    private string[] GetActionNames(InputActionAsset actions)
    {
        var names = new System.Collections.Generic.List<string>();
        foreach (var actionMap in actions.actionMaps)
        {
            foreach (var action in actionMap.actions)
            {
                names.Add(action.name);
            }
        }
        return names.ToArray();
    }

    private void DisableInputActions()
    {
        attackAction?.Disable();
        reloadAction?.Disable();
        switchModeAction?.Disable();
        dropAction?.Disable();

        foreach (var action in abilityActions)
        {
            action?.Disable();
        }
    }

    public void SetGunDataHolder(GameObject holder)
    {
        gunDataHolder = holder;
        if (gunDataHolder != null)
        {
            InitializeWeaponData();
        }
    }

    public void InitializeAmmo()
    {
        Debug.Log($"InitializeAmmo called for {weaponName}, hasBeenPickedUpBefore: {hasBeenPickedUpBefore}");

        if (weaponData != null)
        {
            if (ammoModule != null)
            {
                if (!hasBeenPickedUpBefore)
                {
                    // First pickup - give full ammo
                    Debug.Log($"First pickup - giving full ammo: {weaponData.totalAmmo}");
                    var standardAmmo = ammoModule as StandardAmmoModule;
                    if (standardAmmo != null)
                    {
                        standardAmmo.currentMagazine = weaponData.magazineCapacity;
                        standardAmmo.totalAmmo = weaponData.totalAmmo;
                        standardAmmo.maxMagazine = weaponData.magazineCapacity;
                    }
                    hasBeenPickedUpBefore = true;
                }
                else
                {
                    Debug.Log($"Already picked up before - current ammo: {ammoModule.GetCurrentAmmo()}/{ammoModule.GetTotalAmmo()}");
                }

                // Always re-initialize the module with current weapon data
                ammoModule.Initialize(this);
            }
            else
            {
                Debug.LogError("No ammo module found!");
            }
        }
        else
        {
            Debug.LogError("No weapon data found!");
        }
    }

    private void InitializeWeaponData()
    {
        if (gunDataHolder == null)
        {
            Debug.LogError("Gun data holder is null!");
            return;
        }

        GunData gunData = gunDataHolder.GetComponent<GunData>();
        if (gunData == null)
        {
            Debug.LogError("No GunData component found on gun data holder!");
            return;
        }

        Debug.Log($"Initializing weapon data for tag: {weaponTag}");

        // Set weapon data based on tag
        switch (weaponTag)
        {
            case "MachineGun":
                weaponData = gunData.machineGun;
                weaponSound = soundData?.machineGun;
                break;
            case "ShotGun":
                weaponData = gunData.shotGun;
                weaponSound = soundData?.shotGun;
                break;
            case "Sniper":
                weaponData = gunData.sniper;
                weaponSound = soundData?.sniper;
                break;
            case "HandGun":
                weaponData = gunData.handGun;
                weaponSound = soundData?.handGun;
                break;
            case "SMG":
                weaponData = gunData.smg;
                weaponSound = soundData?.smg;
                break;
            case "BurstRifle":
                weaponData = gunData.burstRifle;
                weaponSound = soundData?.burstRifle;
                break;
            default:
                Debug.LogWarning($"Unknown weapon tag: {weaponTag}, defaulting to machine gun");
                weaponData = gunData.machineGun;
                weaponSound = soundData?.machineGun;
                break;
        }

        if (weaponData != null)
        {
            Debug.Log($"Weapon data loaded - Damage: {weaponData.damage}, Magazine: {weaponData.magazineCapacity}, Total Ammo: {weaponData.totalAmmo}");
            Debug.Log($"Sound data loaded: {weaponSound != null}, Shoot clip: {weaponSound?.shootClip?.name}");

            // Re-initialize all modules with new weapon data
            NotifyModules(module => module.Initialize(this));
        }
        else
        {
            Debug.LogError("Failed to load weapon data!");
        }
    }

    // === INPUT HANDLING ===

    private void HandleInput()
    {
        // Try new input system first
        if (attackAction != null && dropAction != null)
        {
            HandleNewInputSystem();
        }
        else
        {
            // Fallback to old input system
            HandleOldInputSystem();
        }
    }

    private void HandleNewInputSystem()
    {
        // Fire input
        if (fireModule != null && attackAction != null)
        {
            bool isPressed = attackAction.IsPressed();
            bool wasPressed = attackAction.WasPressedThisFrame();

            if (wasPressed)
            {
                Debug.Log($"Attack input detected! CanFire: {fireModule.CanFire()}");
            }

            fireModule.OnFireInput(isPressed, wasPressed);
        }

        // Reload input
        if (reloadAction?.WasPressedThisFrame() == true && ammoModule != null)
        {
            Debug.Log("Reload input detected!");
            if (ammoModule.CanReload())
            {
                StartCoroutine(ammoModule.Reload());
            }
        }

        // Drop weapon input
        if (dropAction?.WasPressedThisFrame() == true)
        {
            Debug.Log("Drop input detected!");
            DropWeapon();
        }

        // Mode switching
        if (switchModeAction?.WasPressedThisFrame() == true)
        {
            Debug.Log("Mode switch pressed");
        }

        // Ability inputs
        for (int i = 0; i < abilityActions.Count && i < abilityModules.Count; i++)
        {
            if (abilityActions[i] != null && abilityActions[i].WasPressedThisFrame())
            {
                if (i < abilityModules.Count && abilityModules[i].CanActivate())
                {
                    abilityModules[i].ActivateAbility();
                }
            }
        }
    }

    private void HandleOldInputSystem()
    {
        // Only process input if weapon is active
        if (!isActiveWeapon)
        {
            return;
        }

        // Fire input using old Input system
        if (fireModule != null)
        {
            bool isPressed = Input.GetMouseButton(0);
            bool wasPressed = Input.GetMouseButtonDown(0);

            if (wasPressed)
            {
                Debug.Log($"Old input - Attack detected! CanFire: {fireModule.CanFire()}");
            }

            fireModule.OnFireInput(isPressed, wasPressed);
        }

        // Reload input
        if (Input.GetKeyDown(KeyCode.R) && ammoModule != null)
        {
            Debug.Log("Old input - Reload detected!");
            if (ammoModule.CanReload())
            {
                StartCoroutine(ammoModule.Reload());
            }
        }

        // Drop weapon input
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Old input - Drop detected!");
            DropWeapon();
        }

        // Mode switching
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Old input - Mode switch pressed");
        }
    }

    private void DropWeapon()
    {
        if (WeaponManager.Instance != null)
        {
            // Stop aiming when dropping
            if (AimingManager.Instance != null && AimingManager.Instance.isAiming)
            {
                AimingManager.Instance.ForceStopAiming();
            }

            // IMPORTANT: Set inactive BEFORE doing anything else
            isActiveWeapon = false;
            NotifyModules(module => module.OnWeaponDeactivated());

            // Disable input actions
            DisableInputActions();

            // Remove from slot
            transform.SetParent(null);

            // Enable physics
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                // Add some force to drop it in front of player
                rb.AddForce(Camera.main.transform.forward * 3f + Vector3.up * 1f, ForceMode.Impulse);
            }

            // Disable animator but don't destroy it
            if (animator != null)
            {
                animator.enabled = false;
            }

            // Clear UI
            if (AmmoManager.Instance?.ammoDisplay != null)
            {
                AmmoManager.Instance.ammoDisplay.text = "-- / --";
            }

            Debug.Log($"Weapon {weaponName} dropped and deactivated");
        }
    }

    private void UpdateUI()
    {
        try
        {
            if (AmmoManager.Instance?.ammoDisplay != null && isActiveWeapon && ammoModule != null)
            {
                AmmoManager.Instance.ammoDisplay.text = $"{ammoModule.GetCurrentAmmo()} / {ammoModule.GetTotalAmmo()}";
            }
            else if (AmmoManager.Instance?.ammoDisplay != null && isActiveWeapon)
            {
                // No ammo module - show infinite ammo
                AmmoManager.Instance.ammoDisplay.text = "∞ / ∞";
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error updating UI: {e.Message}");
        }
    }

    // === PUBLIC METHODS FOR MODULES ===

    public void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void SetAnimationTrigger(string triggerName)
    {
        if (animator != null && HasAnimationParameter(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }

    private bool HasAnimationParameter(string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == paramName)
                return true;
        }
        return false;
    }

    public Vector3 CalculateBaseDirection()
    {
        // Ensure we have a fire point
        if (firePoint == null)
        {
            Debug.LogWarning("Fire point is null! Using weapon transform.");
            firePoint = transform; // Fallback to weapon transform
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
        }

        return (targetPoint - firePoint.position).normalized;
    }

    // === GETTERS FOR MODULES ===

    public T GetModule<T>() where T : class, IWeaponModule
    {
        foreach (var module in allModules)
        {
            if (module is T typedModule)
                return typedModule;
        }
        return null;
    }

    // Legacy method for compatibility
    public void SetupInput()
    {
        SetupInputActions();
    }

    public IFireModule GetFireModule() => fireModule;
    public IProjectileModule GetProjectileModule() => projectileModule;
    public IAmmoModule GetAmmoModule() => ammoModule;
    public ITargetingModule GetTargetingModule() => targetingModule;
    public List<IAbilityModule> GetAbilityModules() => abilityModules;

    // === DEBUG INFO ===
    private void OnValidate()
    {
        // Auto-assign fire point if not set
        if (firePoint == null)
        {
            Transform bulletSpawn = transform.Find("BulletSpawn");
            if (bulletSpawn != null)
            {
                firePoint = bulletSpawn;
            }
        }
    }
}
// end