// FireModeSwitcher.cs - Simple approach without naming conflicts
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeaponFireMode
{
    public string displayName = "Single";
    public bool isActive = true;
    public Color displayColor = Color.white;

    [System.NonSerialized]
    public IFireModule module;
    [System.NonSerialized]
    public bool isCurrentMode = false;
}

public class FireModeSwitcher : MonoBehaviour, IWeaponModule
{
    [Header("Fire Mode Setup")]
    [SerializeField] private List<WeaponFireMode> modes = new List<WeaponFireMode>();

    [Header("Input Settings")]
    public KeyCode switchKey = KeyCode.T;
    public bool useScrollWheel = true;

    [Header("Audio")]
    public AudioClip switchSound;

    // Internal variables
    private ModularWeapon myWeapon;
    private WeaponFireMode currentActiveMode;
    private List<WeaponFireMode> activeModes = new List<WeaponFireMode>();
    private AudioSource myAudio;
    private int modeIndex = 0;

    public void Initialize(ModularWeapon weapon)
    {
        myWeapon = weapon;
        myAudio = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        FindFireModules();
        SetupModes();
        RefreshActiveModes();
        SelectFirstMode();

        Debug.Log($"FireModeSwitcher initialized with {activeModes.Count} modes");
    }

    public void OnWeaponActivated()
    {
        RefreshActiveModes();
        ActivateSelectedMode();
        Debug.Log($"FireModeSwitcher activated - Mode: {currentActiveMode?.displayName ?? "NONE"}");
    }

    public void OnWeaponDeactivated()
    {
        DeactivateAllFireModes();
    }

    public void OnUpdate()
    {
        HandleSwitchInput();
        currentActiveMode?.module?.OnUpdate();
    }

    #region Module Discovery

    private void FindFireModules()
    {
        var foundModules = GetComponents<IFireModule>();

        foreach (var module in foundModules)
        {
            if (module == this) continue; // Skip self

            string moduleName = GetCleanModuleName(module);
            Debug.Log($"Found fire module: {moduleName}");

            module.Initialize(myWeapon);
        }
    }

    private void SetupModes()
    {
        if (modes.Count == 0)
        {
            AutoCreateModes();
        }
        else
        {
            LinkModesToComponents();
        }
    }

    private void AutoCreateModes()
    {
        var foundModules = GetComponents<IFireModule>();

        foreach (var module in foundModules)
        {
            if (module == this) continue;

            WeaponFireMode newMode = new WeaponFireMode
            {
                displayName = GetCleanModuleName(module),
                isActive = true,
                module = module
            };

            modes.Add(newMode);
        }
    }

    private void LinkModesToComponents()
    {
        var foundModules = GetComponents<IFireModule>();

        foreach (var mode in modes)
        {
            if (mode.module == null)
            {
                // Try to find matching module
                foreach (var module in foundModules)
                {
                    if (module == this) continue;

                    string moduleName = GetCleanModuleName(module);
                    if (moduleName.Contains(mode.displayName) || mode.displayName.Contains(moduleName))
                    {
                        mode.module = module;
                        Debug.Log($"Linked '{mode.displayName}' to {module.GetType().Name}");
                        break;
                    }
                }
            }
        }
    }

    private string GetCleanModuleName(IFireModule module)
    {
        string name = module.GetType().Name;
        name = name.Replace("FireModule", "");
        name = name.Replace("Module", "");

        // Add spaces before capitals
        string result = "";
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
                result += " ";
            result += name[i];
        }

        return result.ToUpper();
    }

    #endregion

    #region Mode Management

    private void RefreshActiveModes()
    {
        activeModes.Clear();

        foreach (var mode in modes)
        {
            if (mode.isActive && mode.module != null)
            {
                activeModes.Add(mode);
            }
        }
    }

    private void SelectFirstMode()
    {
        if (activeModes.Count == 0)
        {
            Debug.LogError("No active fire modes found!");
            return;
        }

        modeIndex = 0;
        ChangeToMode(activeModes[0]);
    }

    private void ChangeToMode(WeaponFireMode newMode)
    {
        // Deactivate old mode
        if (currentActiveMode != null)
        {
            currentActiveMode.isCurrentMode = false;
            currentActiveMode.module?.OnWeaponDeactivated();
        }

        // Activate new mode
        currentActiveMode = newMode;
        currentActiveMode.isCurrentMode = true;
        currentActiveMode.module?.OnWeaponActivated();

        modeIndex = activeModes.IndexOf(newMode);

        // Update weapon reference
        UpdateWeaponFireModule();

        // Play sound
        if (switchSound != null && myAudio != null)
        {
            myAudio.PlayOneShot(switchSound);
        }

        Debug.Log($"=== SWITCHED TO: {currentActiveMode.displayName} ===");
    }

    private void UpdateWeaponFireModule()
    {
        try
        {
            var fireModuleField = typeof(ModularWeapon).GetField("fireModule",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fireModuleField != null)
            {
                fireModuleField.SetValue(myWeapon, currentActiveMode?.module);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not update weapon fire module: {e.Message}");
        }
    }

    private void ActivateSelectedMode()
    {
        if (currentActiveMode?.module != null)
        {
            currentActiveMode.module.OnWeaponActivated();
            UpdateWeaponFireModule();
        }
    }

    private void DeactivateAllFireModes()
    {
        foreach (var mode in modes)
        {
            if (mode.module != null)
            {
                mode.module.OnWeaponDeactivated();
                mode.isCurrentMode = false;
            }
        }
    }

    #endregion

    #region Input Handling

    private void HandleSwitchInput()
    {
        // Key switching
        if (Input.GetKeyDown(switchKey))
        {
            SwitchToNext();
        }

        // Scroll wheel switching
        if (useScrollWheel)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.1f)
            {
                SwitchToNext();
            }
            else if (scroll < -0.1f)
            {
                SwitchToPrevious();
            }
        }

        // Number key switching
        for (int i = 0; i < 9 && i < activeModes.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchToIndex(i);
            }
        }
    }

    #endregion

    #region Public Methods

    public void SwitchToNext()
    {
        if (activeModes.Count <= 1) return;

        int nextIndex = (modeIndex + 1) % activeModes.Count;
        SwitchToIndex(nextIndex);
    }

    public void SwitchToPrevious()
    {
        if (activeModes.Count <= 1) return;

        int prevIndex = modeIndex - 1;
        if (prevIndex < 0) prevIndex = activeModes.Count - 1;

        SwitchToIndex(prevIndex);
    }

    public bool SwitchToIndex(int index)
    {
        if (index < 0 || index >= activeModes.Count)
        {
            Debug.LogWarning($"Invalid mode index: {index}");
            return false;
        }

        ChangeToMode(activeModes[index]);
        return true;
    }

    public bool SwitchToMode(string modeName)
    {
        foreach (var mode in activeModes)
        {
            if (mode.displayName.Equals(modeName, System.StringComparison.OrdinalIgnoreCase))
            {
                int index = activeModes.IndexOf(mode);
                return SwitchToIndex(index);
            }
        }

        Debug.LogWarning($"Mode '{modeName}' not found");
        return false;
    }

    // Getters
    public string GetCurrentModeName() => currentActiveMode?.displayName ?? "NONE";
    public IFireModule GetCurrentFireModule() => currentActiveMode?.module;
    public int GetModeCount() => activeModes.Count;
    public int GetCurrentIndex() => modeIndex;

    public List<string> GetModeNames()
    {
        List<string> names = new List<string>();
        foreach (var mode in activeModes)
        {
            names.Add(mode.displayName);
        }
        return names;
    }

    #endregion

    #region Context Menu

    [ContextMenu("Debug Current Mode")]
    public void DebugMode()
    {
        Debug.Log($"=== FIRE MODE DEBUG ===");
        Debug.Log($"Current Mode: {GetCurrentModeName()}");
        Debug.Log($"Module Type: {currentActiveMode?.module?.GetType().Name ?? "NONE"}");
        Debug.Log($"Mode Index: {modeIndex}/{activeModes.Count - 1}");
        Debug.Log($"Available Modes: {string.Join(", ", GetModeNames())}");
    }

    [ContextMenu("Switch Mode")]
    public void DebugSwitch()
    {
        SwitchToNext();
    }

    [ContextMenu("Refresh Modules")]
    public void RefreshModules()
    {
        FindFireModules();
        SetupModes();
        RefreshActiveModes();
        Debug.Log("Fire modules refreshed");
    }

    #endregion
}