// ModularWeapon.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class ModularWeapon : MonoBehaviour
{
    [Header("=== WEAPON CONFIG ===")]
    public ModularWeaponConfig weaponConfig;

    [Header("=== MODULE MANAGEMENT ===")]
    [Tooltip("Only ONE fire module will be active at a time")]
    public List<MonoBehaviour> availableFireModules = new List<MonoBehaviour>();
    [SerializeField] private int activeFireModuleIndex = 0;

    [Header("=== REFERENCES ===")]
    public Transform firePoint;
    public Transform weaponModel;
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    [Header("=== STATE ===")]
    public bool isActiveWeapon = false;

    // === MODULE REFERENCES ===
    private List<IWeaponModule> allModules = new List<IWeaponModule>();
    private IFireModule activeFireModule;
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
    public ModularWeaponConfig Config => weaponConfig;
    public Transform FirePoint => firePoint;
    public Animator WeaponAnimator => animator;
    public AudioSource WeaponAudio => audioSource;
    public PlayerInput PlayerInputRef => playerInput;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        DiscoverModules();
        SetupFireModules();
    }

    private void Start()
    {
        if (weaponConfig != null)
        {
            InitializeModules();
        }
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
        if (!isActiveWeapon || weaponConfig == null) return;

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

    private void SetupFireModules()
    {
        // Auto-discover fire modules if list is empty
        if (availableFireModules.Count == 0)
        {
            var fireModules = GetComponentsInChildren<MonoBehaviour>()
                .Where(m => m is IFireModule)
                .ToList();

            availableFireModules.AddRange(fireModules);
            Debug.Log($"Auto-discovered {fireModules.Count} fire modules");
        }

        // Disable all fire modules except the active one
        ActivateFireModule(activeFireModuleIndex);
    }

    public void RegisterModule(IWeaponModule module)
    {
        if (allModules.Contains(module)) return;

        allModules.Add(module);

        // Don't auto-register fire modules - we manage them manually
        if (module is IFireModule) return;

        if (module is IProjectileModule projectile) projectileModule = projectile;
        if (module is IAmmoModule ammo) ammoModule = ammo;
        if (module is ITargetingModule targeting) targetingModule = targeting;
        if (module is IAbilityModule ability) abilityModules.Add(ability);

        module.Initialize(this);
    }

    // === FIRE MODULE MANAGEMENT ===
    private void ActivateFireModule(int index)
    {
        if (availableFireModules.Count == 0) return;

        // Disable current fire module
        if (activeFireModule != null)
        {
            (activeFireModule as MonoBehaviour).enabled = false;
            activeFireModule.OnWeaponDeactivated();
        }

        // Clamp index
        activeFireModuleIndex = Mathf.Clamp(index, 0, availableFireModules.Count - 1);

        // Enable new fire module
        var newModule = availableFireModules[activeFireModuleIndex];
        if (newModule != null)
        {
            newModule.enabled = true;
            activeFireModule = newModule as IFireModule;

            if (activeFireModule != null)
            {
                activeFireModule.Initialize(this);
                if (isActiveWeapon)
                {
                    activeFireModule.OnWeaponActivated();
                }
            }

            Debug.Log($"Activated fire module: {newModule.GetType().Name}");
        }
    }

    public void SwitchFireModule()
    {
        if (availableFireModules.Count <= 1) return;

        int nextIndex = (activeFireModuleIndex + 1) % availableFireModules.Count;
        ActivateFireModule(nextIndex);

        PlaySound(weaponConfig?.modeSwitchSound);
    }

    // === INITIALIZATION ===
    private void InitializeModules()
    {
        NotifyModules(module => module.Initialize(this));

        // Initialize active fire module
        if (activeFireModule != null)
        {
            activeFireModule.Initialize(this);
        }
    }

    private void SetupInputActions()
    {
        playerInput = GetComponentInParent<PlayerInput>() ?? FindFirstObjectByType<PlayerInput>();

        if (playerInput?.actions != null)
        {
            try { attackAction = playerInput.actions["Attack"]; attackAction?.Enable(); }
            catch { Debug.LogError("Attack action not found"); }

            try { reloadAction = playerInput.actions["Reload"]; reloadAction?.Enable(); }
            catch { Debug.LogError("Reload action not found"); }

            try { switchModeAction = playerInput.actions["SwitchMode"]; switchModeAction?.Enable(); }
            catch { Debug.LogError("SwitchMode action not found"); }

            try { dropAction = playerInput.actions["DropItem"]; dropAction?.Enable(); }
            catch { Debug.LogError("DropItem action not found"); }

            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            isActiveWeapon = true;
            NotifyModules(module => module.OnWeaponActivated());

            Debug.Log($"=== WEAPON ACTIVATED: {weaponConfig?.weaponName} ===");
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
        // Fire module switching
        if (switchModeAction?.WasPressedThisFrame() == true || Input.GetKeyDown(KeyCode.T))
        {
            SwitchFireModule();
        }

        // Reload
        if (reloadAction?.WasPressedThisFrame() == true && ammoModule != null)
        {
            if (ammoModule.CanReload())
            {
                StartCoroutine(ammoModule.Reload());
            }
        }

        // Drop weapon
        if (dropAction?.WasPressedThisFrame() == true || Input.GetKeyDown(KeyCode.G))
        {
            DropWeapon();
        }

        // Fire input - pass to active fire module
        if (activeFireModule != null)
        {
            bool isPressed = attackAction?.IsPressed() ?? Input.GetMouseButton(0);
            bool wasPressed = attackAction?.WasPressedThisFrame() ?? Input.GetMouseButtonDown(0);

            activeFireModule.OnFireInput(isPressed, wasPressed);
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

        // Also notify active fire module
        try
        {
            if (activeFireModule != null)
            {
                action?.Invoke(activeFireModule);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in fire module {activeFireModule?.GetType().Name}: {e.Message}");
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
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        isActiveWeapon = false;
        NotifyModules(module => module.OnWeaponDeactivated());
        DisableInputActions();

        transform.SetParent(null);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(Camera.main.transform.forward * 3f + Vector3.up * 1f, ForceMode.Impulse);
        }

        if (animator != null) animator.enabled = false;

        Debug.Log($"Dropped weapon");
    }

    // === GETTERS ===
    public T GetModule<T>() where T : class, IWeaponModule
    {
        foreach (var module in allModules)
            if (module is T typedModule) return typedModule;

        return activeFireModule as T;
    }

    public IFireModule GetFireModule() => activeFireModule;
    public IProjectileModule GetProjectileModule() => projectileModule;
    public IAmmoModule GetAmmoModule() => ammoModule;
    public ITargetingModule GetTargetingModule() => targetingModule;
    public List<IAbilityModule> GetAbilityModules() => abilityModules;

    // === INSPECTOR METHODS ===
    [ContextMenu("Switch Fire Module")]
    public void EditorSwitchFireModule() => SwitchFireModule();

    [ContextMenu("Refresh Modules")]
    public void EditorRefreshModules()
    {
        DiscoverModules();
        SetupFireModules();
    }

    public string GetActiveFireModuleName()
    {
        return activeFireModule?.GetType().Name ?? "None";
    }

    private void OnValidate()
    {
        if (firePoint == null)
            firePoint = transform.Find("BulletSpawn") ?? transform;

        if (weaponModel == null)
            weaponModel = transform.Find("GunModel");

        // Clamp active fire module index
        if (availableFireModules.Count > 0)
            activeFireModuleIndex = Mathf.Clamp(activeFireModuleIndex, 0, availableFireModules.Count - 1);
    }
}