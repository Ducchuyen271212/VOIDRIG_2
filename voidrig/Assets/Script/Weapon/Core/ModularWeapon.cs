//ModularWeapon.cs - Complete Fixed Version
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
    public bool isActiveWeapon = false;
    private bool hasBeenPickedUpBefore = false;

    [Header("Core Data")]
    [SerializeField] private GameObject gunDataHolder;
    [SerializeField] private SoundData soundData;

    [Header("Debug - Manual Controls")]
    [SerializeField] private bool useManualDrop = true;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private KeyCode modeKey = KeyCode.T;

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
    private List<InputAction> abilityActions = new List<InputAction>();

    // Weapon Data
    private GunData.Attribute weaponData;
    private SoundData.WeaponSound weaponSound;

    // Properties for modules to access
    public GunData.Attribute WeaponData => weaponData;
    public SoundData.WeaponSound WeaponSound => weaponSound;
    public Transform FirePoint => firePoint;
    public Animator WeaponAnimator => animator;
    public AudioSource WeaponAudio => audioSource;
    public PlayerInput PlayerInputRef => playerInput; // Added for modules to access

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        DiscoverModules();
    }

    private void Start()
    {
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
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (playerInput?.actions != null)
        {
            Debug.Log($"Found PlayerInput: {playerInput.name}");

            try { attackAction = playerInput.actions["Attack"]; attackAction?.Enable(); }
            catch { Debug.LogError("Attack action not found"); }

            try { reloadAction = playerInput.actions["Reload"]; reloadAction?.Enable(); }
            catch { Debug.LogError("Reload action not found"); }

            try { switchModeAction = playerInput.actions["SwitchMode"]; switchModeAction?.Enable(); }
            catch { Debug.LogError("SwitchMode action not found"); }

            try { dropAction = playerInput.actions["DropItem"]; dropAction?.Enable(); }
            catch { Debug.LogError("DropItem action not found"); }

            Debug.Log($"Input actions enabled - Attack: {attackAction?.enabled}, Reload: {reloadAction?.enabled}, Drop: {dropAction?.enabled}");

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
                        abilityActions.Add(null);
                    }
                }
                catch
                {
                    abilityActions.Add(null);
                }
            }

            // DISABLE COLLIDER WHEN EQUIPPED
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                Debug.Log($"Disabled collider for {weaponName}");
            }

            isActiveWeapon = true;
            NotifyModules(module => module.OnWeaponActivated());

            Debug.Log($"=== WEAPON FULLY ACTIVATED: {weaponName} ===");
        }
        else
        {
            Debug.LogError($"PlayerInput not found! Cannot setup input actions.");
        }
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
        Debug.Log($"=== InitializeWeaponData called ===");
        Debug.Log($"Gun data holder: {gunDataHolder?.name}");
        Debug.Log($"Weapon tag: '{weaponTag}'");

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
        // Always check manual keys first
        if (useManualDrop && Input.GetKeyDown(dropKey))
        {
            Debug.Log($"=== MANUAL DROP KEY ({dropKey}) PRESSED ===");
            DropWeapon();
            return;
        }

        // Try Input System actions
        HandleInputSystemActions();

        // Handle fire input (this is critical!)
        if (fireModule != null)
        {
            bool isPressed = false;
            bool wasPressed = false;

            // Try Input System first
            if (attackAction != null)
            {
                isPressed = attackAction.IsPressed();
                wasPressed = attackAction.WasPressedThisFrame();
            }
            else
            {
                // Fallback to old input
                isPressed = Input.GetMouseButton(0);
                wasPressed = Input.GetMouseButtonDown(0);
            }

            // Always send input to fire module
            fireModule.OnFireInput(isPressed, wasPressed);
        }
    }

    private void HandleInputSystemActions()
    {
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
            Debug.Log("=== INPUT SYSTEM DROP PRESSED ===");
            DropWeapon();
        }

        // Mode switching - handled by fire module, but we can also handle it here
        if (switchModeAction?.WasPressedThisFrame() == true || Input.GetKeyDown(modeKey))
        {
            Debug.Log("=== MODE SWITCH PRESSED ===");
            var flexibleModule = fireModule as FlexibleFireModule;
            if (flexibleModule != null)
            {
                flexibleModule.SwitchMode();
            }
        }
    }

    private void DropWeapon()
    {
        Debug.Log($"=== WEAPON DROP TRIGGERED === for {weaponName}");

        // RE-ENABLE COLLIDER WHEN DROPPED
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log($"Re-enabled collider for {weaponName}");
        }

        if (WeaponManager.Instance != null)
        {
            if (AimingManager.Instance != null && AimingManager.Instance.isAiming)
            {
                AimingManager.Instance.ForceStopAiming();
            }

            isActiveWeapon = false;
            NotifyModules(module => module.OnWeaponDeactivated());
            DisableInputActions();

            transform.SetParent(null);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(Camera.main.transform.forward * 3f + Vector3.up * 1f, ForceMode.Impulse);
            }

            if (animator != null)
            {
                animator.enabled = false;
            }

            if (AmmoManager.Instance?.ammoDisplay != null)
            {
                AmmoManager.Instance.ammoDisplay.text = "-- / --";
            }

            Debug.Log($"Weapon {weaponName} dropped and deactivated");
        }
        else
        {
            Debug.LogError("WeaponManager.Instance is null!");
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
        if (firePoint == null)
        {
            Debug.LogWarning("Fire point is null! Using weapon transform.");
            firePoint = transform;
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

    [ContextMenu("Drop Weapon")]
    public void ManualDropWeapon()
    {
        Debug.Log("=== MANUAL DROP CALLED ===");
        DropWeapon();
    }

    public void SetupInput()
    {
        SetupInputActions();
    }

    public IFireModule GetFireModule() => fireModule;
    public IProjectileModule GetProjectileModule() => projectileModule;
    public IAmmoModule GetAmmoModule() => ammoModule;
    public ITargetingModule GetTargetingModule() => targetingModule;
    public List<IAbilityModule> GetAbilityModules() => abilityModules;

    private void OnValidate()
    {
        if (firePoint == null)
        {
            Transform bulletSpawn = transform.Find("BulletSpawn");
            if (bulletSpawn != null)
            {
                firePoint = bulletSpawn;
            }
        }

        if (weaponModel == null)
        {
            weaponModel = transform.Find("GunModel");
            if (weaponModel == null)
            {
                Transform[] children = GetComponentsInChildren<Transform>();
                foreach (var child in children)
                {
                    if (child.name.ToLower().Contains("model") || child.name.ToLower().Contains("gun"))
                    {
                        weaponModel = child;
                        break;
                    }
                }
            }
        }
    }
}
// end