// FireModeController.cs
using UnityEngine;
using System.Collections.Generic;

public class FireModeController : MonoBehaviour, IWeaponModule
{
    [Header("Available Fire Modules")]
    public List<MonoBehaviour> fireModules = new List<MonoBehaviour>();

    [Header("Current Settings")]
    public int currentModeIndex = 0;

    private ModularWeapon weapon;
    private IFireModule currentFireModule;

    public void Initialize(ModularWeapon weapon)
    {
        this.weapon = weapon;

        // Find all fire modules automatically
        if (fireModules.Count == 0)
        {
            AutoDiscoverFireModules();
        }

        // Set initial mode
        SwitchToMode(currentModeIndex);
    }

    public void OnWeaponActivated()
    {
        currentFireModule?.OnWeaponActivated();
    }

    public void OnWeaponDeactivated()
    {
        currentFireModule?.OnWeaponDeactivated();
    }

    public void OnUpdate()
    {
        // Handle mode switching input
        if (Input.GetKeyDown(KeyCode.T))
        {
            SwitchToNextMode();
        }

        currentFireModule?.OnUpdate();
    }

    private void AutoDiscoverFireModules()
    {
        // Find all fire modules on this weapon
        var singleFire = GetComponent<SingleFireModule>();
        var burstFire = GetComponent<BurstFireModule>();
        var autoFire = GetComponent<AutoFireModule>();
        var flexibleFire = GetComponent<FlexibleFireModule>();

        if (singleFire != null) fireModules.Add(singleFire);
        if (burstFire != null) fireModules.Add(burstFire);
        if (autoFire != null) fireModules.Add(autoFire);
        if (flexibleFire != null) fireModules.Add(flexibleFire);

        Debug.Log($"Found {fireModules.Count} fire modules");
    }

    public void SwitchToMode(int modeIndex)
    {
        if (fireModules.Count == 0) return;

        // Disable current module
        if (currentFireModule != null)
        {
            (currentFireModule as MonoBehaviour).enabled = false;
            currentFireModule.OnWeaponDeactivated();
        }

        // Clamp index
        currentModeIndex = Mathf.Clamp(modeIndex, 0, fireModules.Count - 1);

        // Enable new module
        var newModule = fireModules[currentModeIndex];
        newModule.enabled = true;
        currentFireModule = newModule as IFireModule;

        // Update weapon's fire module reference
        UpdateWeaponFireModule();

        currentFireModule?.OnWeaponActivated();

        Debug.Log($"Switched to fire mode: {newModule.GetType().Name}");
    }

    public void SwitchToNextMode()
    {
        int nextIndex = (currentModeIndex + 1) % fireModules.Count;
        SwitchToMode(nextIndex);
    }

    private void UpdateWeaponFireModule()
    {
        // Use reflection to update the weapon's fireModule field
        var field = typeof(ModularWeapon).GetField("fireModule",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(weapon, currentFireModule);
    }

    // Public methods for external control
    public void SetSingleFireMode()
    {
        var singleModule = fireModules.Find(m => m is SingleFireModule);
        if (singleModule != null)
        {
            SwitchToMode(fireModules.IndexOf(singleModule));
        }
    }

    public void SetBurstFireMode()
    {
        var burstModule = fireModules.Find(m => m is BurstFireModule);
        if (burstModule != null)
        {
            SwitchToMode(fireModules.IndexOf(burstModule));
        }
    }

    public void SetAutoFireMode()
    {
        var autoModule = fireModules.Find(m => m is AutoFireModule);
        if (autoModule != null)
        {
            SwitchToMode(fireModules.IndexOf(autoModule));
        }
    }

    public string GetCurrentModeName()
    {
        if (currentFireModule != null)
        {
            return currentFireModule.GetType().Name.Replace("FireModule", "");
        }
        return "None";
    }

    // Inspector button methods
    [ContextMenu("Switch to Single")]
    public void EditorSwitchToSingle() => SetSingleFireMode();

    [ContextMenu("Switch to Burst")]
    public void EditorSwitchToBurst() => SetBurstFireMode();

    [ContextMenu("Switch to Auto")]
    public void EditorSwitchToAuto() => SetAutoFireMode();
}