// FlexibleFireModule.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class FlexibleFireModule : MonoBehaviour, IFireModule
{
    [Header("Fire Mode Management")]
    [SerializeField] private List<IFireModule> discoveredFireModules = new List<IFireModule>();
    [SerializeField] private List<string> fireModuleNames = new List<string>();
    [SerializeField] private int currentModeIndex = 0;

    [Header("Display")]
    public bool showCurrentModeInConsole = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private ModularWeapon weapon;
    private IFireModule currentFireModule;
    private bool hasDiscoveredModules = false;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;
        DiscoverFireModules();
        DebugLog($"FlexibleFireModule initialized with {discoveredFireModules.Count} fire modes");
    }

    private void DiscoverFireModules()
    {
        discoveredFireModules.Clear();
        fireModuleNames.Clear();

        // Get all components that implement IFireModule
        var allFireModules = weapon.GetComponents<IFireModule>();

        foreach (var module in allFireModules)
        {
            // Skip self
            if (module == this) continue;

            // Skip disabled components
            var behaviour = module as MonoBehaviour;
            if (behaviour != null && !behaviour.enabled) continue;

            discoveredFireModules.Add(module);
            string moduleName = module.GetType().Name.Replace("FireModule", "").Replace("Module", "");
            fireModuleNames.Add(moduleName);

            DebugLog($"Discovered fire module: {moduleName}");
        }

        if (discoveredFireModules.Count > 0)
        {
            currentModeIndex = 0;
            currentFireModule = discoveredFireModules[0];
            hasDiscoveredModules = true;

            // Initialize all discovered modules
            foreach (var module in discoveredFireModules)
            {
                module.Initialize(weapon);
            }
        }
        else
        {
            Debug.LogWarning("No fire modules found! Add SingleFireModule, BurstFireModule, or AutoFireModule components.");
        }
    }

    public void OnWeaponActivated()
    {
        // Re-discover modules in case any were added/removed
        DiscoverFireModules();

        if (currentFireModule != null)
        {
            currentFireModule.OnWeaponActivated();
            DebugLog($"Activated with {fireModuleNames[currentModeIndex]} mode");
        }
    }

    public void OnWeaponDeactivated()
    {
        if (currentFireModule != null)
        {
            currentFireModule.OnWeaponDeactivated();
        }
    }

    public void OnUpdate()
    {
        if (!hasDiscoveredModules) return;

        // Handle mode switching via InputSystem
        if (weapon.PlayerInputRef?.actions != null)
        {
            var switchAction = weapon.PlayerInputRef.actions["SwitchMode"];
            if (switchAction?.WasPressedThisFrame() == true)
            {
                SwitchToNextMode();
            }
        }

        // Update current fire module
        currentFireModule?.OnUpdate();
    }

    public bool CanFire()
    {
        return currentFireModule?.CanFire() ?? false;
    }

    public void OnFireInput(bool isPressed, bool wasPressed)
    {
        currentFireModule?.OnFireInput(isPressed, wasPressed);
    }

    public IEnumerator Fire()
    {
        if (currentFireModule != null)
        {
            yield return currentFireModule.Fire();
        }
    }

    private void SwitchToNextMode()
    {
        if (discoveredFireModules.Count <= 1) return;

        // Deactivate current module
        currentFireModule?.OnWeaponDeactivated();

        // Move to next module
        currentModeIndex = (currentModeIndex + 1) % discoveredFireModules.Count;
        currentFireModule = discoveredFireModules[currentModeIndex];

        // Activate new module
        currentFireModule?.OnWeaponActivated();

        string modeName = fireModuleNames[currentModeIndex];

        if (showCurrentModeInConsole)
        {
            Debug.Log($"=== SWITCHED TO {modeName.ToUpper()} MODE ===");
        }

        DebugLog($"Fire mode switched to: {modeName}");
    }

    // Public methods for external control
    public void SwitchToMode(int index)
    {
        if (index < 0 || index >= discoveredFireModules.Count) return;

        currentFireModule?.OnWeaponDeactivated();
        currentModeIndex = index;
        currentFireModule = discoveredFireModules[index];
        currentFireModule?.OnWeaponActivated();

        DebugLog($"Switched to {fireModuleNames[index]} mode");
    }

    public void SwitchToModeByName(string modeName)
    {
        for (int i = 0; i < fireModuleNames.Count; i++)
        {
            if (fireModuleNames[i].Equals(modeName, System.StringComparison.OrdinalIgnoreCase))
            {
                SwitchToMode(i);
                return;
            }
        }

        Debug.LogWarning($"Fire mode '{modeName}' not found");
    }

    // Getters
    public string GetCurrentModeName() => fireModuleNames.Count > 0 ? fireModuleNames[currentModeIndex] : "None";
    public int GetCurrentModeIndex() => currentModeIndex;
    public int GetModeCount() => discoveredFireModules.Count;
    public List<string> GetAvailableModes() => new List<string>(fireModuleNames);

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[FlexibleFireModule] {message}");
    }

    // Editor helpers
    [ContextMenu("Refresh Fire Modules")]
    public void RefreshFireModules()
    {
        DiscoverFireModules();
        Debug.Log($"Refreshed: Found {discoveredFireModules.Count} fire modules");
    }

    [ContextMenu("Debug Current State")]
    public void DebugCurrentState()
    {
        Debug.Log("=== FLEXIBLE FIRE MODULE STATE ===");
        Debug.Log($"Current Mode: {GetCurrentModeName()} (Index: {currentModeIndex})");
        Debug.Log($"Available Modes: {string.Join(", ", fireModuleNames)}");
        Debug.Log($"Total Modes: {discoveredFireModules.Count}");
    }
}
// end