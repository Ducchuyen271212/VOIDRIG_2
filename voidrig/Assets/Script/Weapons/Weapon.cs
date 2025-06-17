
//Weapon.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public GameObject muzzleEffect;

    [Header("Data")]
    [SerializeField] private GameObject gunDataHolder;
    [SerializeField] private SoundData soundData;

    [Header("Positioning")]
    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    [Header("State")]
    public bool isActiveWeapon = false;

    private GunData.Attribute activeGun;
    private SoundData.WeaponSound activeSound;

    private int currentAmmo;
    private int magazineCapacity;
    private int totalAmmo;
    private float reloadTime;
    private float fireRate;
    private float bulletVelocity;
    private float bulletLifeTime;
    private float spreadIntensity;
    private int bulletsPerBurst;
    private float burstFireInterval;

    private GunData.ShootingMode[] availableModes;
    private int currentModeIndex;
    private GunData.ShootingMode currentShootingMode;
    private int burstBulletsLeft;

    private bool isShooting = false;
    private bool readyToShoot = true;
    private bool allowReset = true;
    private bool isReloading = false;

    private float lastShotTime = 0f;

    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction reloadAction;
    private InputAction switchModeAction;

    private Animator animator;
    private AudioSource audioSource;

    private void Awake()
    {
        readyToShoot = true;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        if (gunDataHolder != null)
        {
            InitializeWeaponData();
        }
        else
        {
            Debug.LogWarning($"GunDataHolder not set for weapon: {gameObject.name}. Will be set by WeaponManager.");
        }
    }

    private void OnEnable()
    {
        // Only setup input if weapon is in a player slot
        if (transform.parent != null && transform.parent.name.Contains("Slot"))
        {
            playerInput = GetComponentInParent<PlayerInput>();
            if (playerInput == null || playerInput.actions == null)
            {
                Debug.LogWarning("PlayerInput or its actions are not initialized for weapon in slot!");
                return;
            }

            attackAction = playerInput.actions["Attack"];
            reloadAction = playerInput.actions["Reload"];
            switchModeAction = playerInput.actions["SwitchMode"];

            attackAction?.Enable();
            reloadAction?.Enable();
            switchModeAction?.Enable();

            ResetWeaponState();
            isActiveWeapon = true;
        }
        else
        {
            // Weapon is dropped/not in slot
            isActiveWeapon = false;
        }
    }

    private void OnDisable()
    {
        isActiveWeapon = false;
    }

    public void SetGunDataHolder(GameObject holder)
    {
        gunDataHolder = holder;
        if (gunDataHolder != null)
        {
            InitializeWeaponData();
        }
    }

    // Initialize ammo when weapon is picked up
    public void InitializeAmmo()
    {
        if (activeGun != null)
        {
            currentAmmo = magazineCapacity;
            totalAmmo = activeGun.totalAmmo;
            isReloading = false;
            Debug.Log($"Weapon ammo initialized: {currentAmmo}/{totalAmmo}");
        }
        else
        {
            Debug.LogWarning("Cannot initialize ammo - weapon data not set yet");
        }
    }

    private void InitializeWeaponData()
    {
        if (gunDataHolder == null)
        {
            Debug.LogError($"GunDataHolder not set for weapon: {gameObject.name}");
            return;
        }

        GunData gunData = gunDataHolder.GetComponent<GunData>();
        if (gunData == null)
        {
            Debug.LogError($"GunData component not found on GunDataHolder for weapon: {gameObject.name}");
            return;
        }

        switch (gameObject.tag)
        {
            case "MachineGun": SetActiveGun(gunData.machineGun, soundData?.machineGun); break;
            case "ShotGun": SetActiveGun(gunData.shotGun, soundData?.shotGun); break;
            case "Sniper": SetActiveGun(gunData.sniper, soundData?.sniper); break;
            case "HandGun": SetActiveGun(gunData.handGun, soundData?.handGun); break;
            case "SMG": SetActiveGun(gunData.smg, soundData?.smg); break;
            case "BurstRifle": SetActiveGun(gunData.burstRifle, soundData?.burstRifle); break;
            default:
                Debug.LogWarning("Unknown weapon tag: " + gameObject.tag + ". Defaulting to MachineGun.");
                SetActiveGun(gunData.machineGun, soundData?.machineGun);
                break;
        }
    }

    private void ResetWeaponState()
    {
        readyToShoot = true;
        isShooting = false;
        isReloading = false;
        allowReset = true;
        lastShotTime = 0f;

        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }

        Debug.Log($"Reset weapon state for: {gameObject.name}");
    }

    private void Update()
    {
        // Only process input if weapon is active
        if (!isActiveWeapon)
        {
            return;
        }

        // Safety checks
        if (attackAction == null || reloadAction == null || switchModeAction == null || activeGun == null)
        {
            return;
        }

        bool canShootByFireRate = Time.time >= lastShotTime + fireRate;

        isShooting = currentShootingMode == GunData.ShootingMode.Auto
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame();

        if (readyToShoot && isShooting && canShootByFireRate && !isReloading)
        {
            if (currentAmmo > 0)
            {
                lastShotTime = Time.time;

                if (activeGun.scatter || currentShootingMode == GunData.ShootingMode.Single)
                {
                    PlaySound(soundData?.GetShootClip(activeSound));
                }

                if (animator != null && animator.GetCurrentAnimatorClipInfo(0).Length > 0 &&
                    animator.GetCurrentAnimatorClipInfo(0)[0].clip.name != "Recoil")
                {
                    SetAnimTrigger("Recoil");
                }

                burstBulletsLeft = bulletsPerBurst;
                StartCoroutine(ShootRepeatedly());
            }
            else
            {
                SetAnimTrigger("RecoilRecover");
                PlaySound(soundData?.GetEmptyClip(activeSound));
            }
        }

        if (reloadAction.WasPressedThisFrame())
        {
            TryReload();
        }

        if (switchModeAction.WasPressedThisFrame())
        {
            SwitchMode();
        }

        // Only update ammo display if this weapon is active
        if (AmmoManager.Instance?.ammoDisplay != null && isActiveWeapon)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{currentAmmo} / {totalAmmo}";
        }
    }

    private IEnumerator ShootRepeatedly()
    {
        if (currentShootingMode != GunData.ShootingMode.Auto)
        {
            readyToShoot = false;
        }

        if (activeGun.scatter)
        {
            if (currentAmmo > 0 && !isReloading)
            {
                for (int i = 0; i < bulletsPerBurst; i++)
                {
                    FireBullet(ignoreAmmo: true);
                }
                currentAmmo--;
            }
        }
        else
        {
            while (burstBulletsLeft > 0 && currentAmmo > 0 && !isReloading)
            {
                FireBullet();

                if (currentShootingMode == GunData.ShootingMode.Burst || currentShootingMode == GunData.ShootingMode.Auto)
                {
                    PlaySound(soundData?.GetShootClip(activeSound));
                }

                burstBulletsLeft--;

                if (currentShootingMode == GunData.ShootingMode.Single)
                    break;

                yield return new WaitForSeconds(burstFireInterval);
            }
        }

        if (currentShootingMode == GunData.ShootingMode.Auto)
        {
            if (attackAction.WasReleasedThisFrame() || currentAmmo <= 0)
            {
                ResetShot();
            }
            else
            {
                if (animator != null)
                {
                    animator.Play("Idle", 0, 0f);
                }
            }
        }
        else
        {
            ResetShot();
        }
    }

    private void FireBullet(bool ignoreAmmo = false)
    {
        if (!ignoreAmmo && currentAmmo <= 0)
        {
            Debug.Log(totalAmmo <= 0 ? "Completely out of ammo!" : "Magazine empty! Press reload.");
            return;
        }

        SetAnimTrigger("RecoilHigh");
        muzzleEffect?.GetComponent<ParticleSystem>()?.Play();

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

        if (bullet.TryGetComponent(out Bullet bulletScript))
        {
            bulletScript.damage = activeGun.damage;
        }

        bullet.transform.forward = shootingDirection;
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);
        Destroy(bullet, bulletLifeTime);

        if (!ignoreAmmo)
            currentAmmo--;

        Debug.Log("Bullet fired! Remaining ammo: " + currentAmmo + "/" + totalAmmo);
    }

    private void TryReload()
    {
        if (!isReloading)
        {
            isReloading = true;
            Debug.Log("Reloading...");
            SetAnimTrigger("Reload");
            PlaySound(soundData?.GetReloadClip(activeSound));
            StartCoroutine(ReloadWeapon(reloadTime));
        }
    }

    private IEnumerator ReloadWeapon(float reloadTime)
    {
        yield return new WaitForSeconds(reloadTime);

        int missingAmmo = magazineCapacity - currentAmmo;

        if (totalAmmo <= 0)
        {
            Debug.Log("No ammo in reserve to reload!");
        }
        else if (totalAmmo < missingAmmo)
        {
            currentAmmo += totalAmmo;
            totalAmmo = 0;
        }
        else
        {
            currentAmmo = magazineCapacity;
            totalAmmo -= missingAmmo;
        }

        Debug.Log("Reloaded. Ammo: " + currentAmmo + "/" + totalAmmo);
        SetAnimTrigger("ReloadRecover");
        isReloading = false;
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
        SetAnimTrigger("RecoilRecover");

        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }
    }

    private void SetActiveGun(GunData.Attribute gun, SoundData.WeaponSound sound)
    {
        activeGun = gun;
        activeSound = sound;

        magazineCapacity = gun.magazineCapacity;
        currentAmmo = magazineCapacity;
        totalAmmo = gun.totalAmmo;
        reloadTime = gun.reloadTime;
        fireRate = gun.fireRate;
        bulletVelocity = gun.bulletVelocity;
        bulletLifeTime = gun.bulletLifeTime;
        spreadIntensity = gun.spreadIntensity;
        bulletsPerBurst = Mathf.Max(1, gun.bulletsPerBurst);
        burstFireInterval = gun.burstFireInterval;

        availableModes = gun.availableModes != null && gun.availableModes.Length > 0
            ? gun.availableModes
            : new GunData.ShootingMode[] { gun.shootingMode };

        currentModeIndex = 0;
        currentShootingMode = availableModes[currentModeIndex];
        burstBulletsLeft = bulletsPerBurst;

        Debug.Log($"Equipped weapon: {gun} | Mode: {currentShootingMode} | Ammo: {currentAmmo}/{totalAmmo}");
    }

    public void SwitchMode()
    {
        if (availableModes != null && availableModes.Length > 1)
        {
            currentModeIndex = (currentModeIndex + 1) % availableModes.Length;
            currentShootingMode = availableModes[currentModeIndex];
            Debug.Log("Switched shooting mode to: " + currentShootingMode);
        }
        else
        {
            Debug.Log($"Only one firing mode available for {gameObject.name}");
        }
    }

    private Vector3 CalculateDirectionAndSpread()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit) ? hit.point : ray.GetPoint(100);
        Vector3 direction = (targetPoint - bulletSpawn.position).normalized;

        float accuracyMultiplier = (2f - activeGun.accuracy) * 5f;
        float finalSpread = spreadIntensity * accuracyMultiplier;

        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-finalSpread, finalSpread),
            Random.Range(-finalSpread, finalSpread),
            0
        );

        return spreadRotation * direction;
    }

    private void SetAnimTrigger(string triggerName)
    {
        if (animator == null)
        {
            Debug.LogError("Animator not assigned.");
            return;
        }

        if (!animator.HasParameterOfType(triggerName, AnimatorControllerParameterType.Trigger))
        {
            Debug.LogWarning($"Missing trigger in Animator: {triggerName}");
            return;
        }

        Debug.Log($"Setting animation trigger: {triggerName}");
        animator.SetTrigger(triggerName);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }
}

public static class AnimatorExtensions
{
    public static bool HasParameterOfType(this Animator animator, string name, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
            if (param.name == name && param.type == type)
                return true;
        return false;
    }
}