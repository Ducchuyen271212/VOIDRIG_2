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
    public Vector3 spawnPosition; // Position when in player's hand
    public Vector3 spawnRotation; // Rotation when in player's hand

    [Header("State")]
    public bool isActiveWeapon = false; // Is weapon currently active/held
    private bool hasBeenPickedUpBefore = false; // Track first pickup for ammo

    // Weapon stats
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

    // Shooting modes
    private GunData.ShootingMode[] availableModes;
    private int currentModeIndex;
    private GunData.ShootingMode currentShootingMode;
    private int burstBulletsLeft;

    // Shooting state
    private bool isShooting = false;
    private bool readyToShoot = true;
    private bool isReloading = false;
    private float lastShotTime = 0f;

    // Input
    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction reloadAction;
    private InputAction switchModeAction;

    // Components
    private Animator animator;
    private AudioSource audioSource;

    // Public accessors for other scripts
    public int CurrentAmmo => currentAmmo;
    public int TotalAmmo => totalAmmo;

    private void Awake()
    {
        readyToShoot = true;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // Initialize weapon data if already set
        if (gunDataHolder != null)
        {
            InitializeWeaponData();
        }
    }

    private void OnEnable()
    {
        // Setup input when weapon is enabled and in a slot
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
    }

    // Setup input actions
    private void SetupInputActions()
    {
        playerInput = GetComponentInParent<PlayerInput>();
        if (playerInput?.actions != null)
        {
            attackAction = playerInput.actions["Attack"];
            reloadAction = playerInput.actions["Reload"];
            switchModeAction = playerInput.actions["SwitchMode"];

            attackAction?.Enable();
            reloadAction?.Enable();
            switchModeAction?.Enable();

            ResetWeaponState();
            isActiveWeapon = true;
        }
    }

    // Public method to manually setup input when weapon is picked up
    public void SetupInput()
    {
        SetupInputActions();
    }

    // Set gun data holder from WeaponManager
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
            // Only give full ammo on first pickup
            if (!hasBeenPickedUpBefore)
            {
                currentAmmo = magazineCapacity;
                totalAmmo = activeGun.totalAmmo;
                hasBeenPickedUpBefore = true;
            }
            isReloading = false;
        }
    }

    // Initialize weapon data based on tag
    private void InitializeWeaponData()
    {
        if (gunDataHolder == null) return;

        GunData gunData = gunDataHolder.GetComponent<GunData>();
        if (gunData == null) return;

        // Set weapon data based on tag
        switch (gameObject.tag)
        {
            case "MachineGun": SetActiveGun(gunData.machineGun, soundData?.machineGun); break;
            case "ShotGun": SetActiveGun(gunData.shotGun, soundData?.shotGun); break;
            case "Sniper": SetActiveGun(gunData.sniper, soundData?.sniper); break;
            case "HandGun": SetActiveGun(gunData.handGun, soundData?.handGun); break;
            case "SMG": SetActiveGun(gunData.smg, soundData?.smg); break;
            case "BurstRifle": SetActiveGun(gunData.burstRifle, soundData?.burstRifle); break;
            default:
                SetActiveGun(gunData.machineGun, soundData?.machineGun);
                break;
        }
    }

    // Reset weapon state when becoming active
    private void ResetWeaponState()
    {
        readyToShoot = true;
        isShooting = false;
        isReloading = false;
        lastShotTime = 0f;

        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }
    }

    private void Update()
    {
        // Only process input if weapon is active and all required components exist
        if (!isActiveWeapon || attackAction == null || reloadAction == null || switchModeAction == null || activeGun == null)
            return;

        // Fire rate control
        bool canShootByFireRate = Time.time >= lastShotTime + fireRate;

        // Check for shooting input
        isShooting = currentShootingMode == GunData.ShootingMode.Auto
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame();

        // Handle shooting
        if (readyToShoot && isShooting && canShootByFireRate && !isReloading)
        {
            if (currentAmmo > 0)
            {
                lastShotTime = Time.time;

                // Play sound for single shots and shotguns
                if (activeGun.scatter || currentShootingMode == GunData.ShootingMode.Single)
                {
                    PlaySound(soundData?.GetShootClip(activeSound));
                }

                // Play recoil animation
                if (animator?.GetCurrentAnimatorClipInfo(0)?.Length > 0 &&
                    animator.GetCurrentAnimatorClipInfo(0)[0].clip.name != "Recoil")
                {
                    SetAnimTrigger("Recoil");
                }

                burstBulletsLeft = bulletsPerBurst;
                StartCoroutine(ShootRepeatedly());
            }
            else
            {
                // Out of ammo
                SetAnimTrigger("RecoilRecover");
                PlaySound(soundData?.GetEmptyClip(activeSound));
            }
        }

        // Handle reload input
        if (reloadAction.WasPressedThisFrame())
        {
            TryReload();
        }

        // Handle mode switching
        if (switchModeAction.WasPressedThisFrame())
        {
            SwitchMode();
        }

        // Update ammo display only if this weapon is active
        if (AmmoManager.Instance?.ammoDisplay != null && isActiveWeapon)
        {
            AmmoManager.Instance.ammoDisplay.text = $"{currentAmmo} / {totalAmmo}";
        }
    }

    // Handle repeated shooting for burst/auto
    private IEnumerator ShootRepeatedly()
    {
        if (currentShootingMode != GunData.ShootingMode.Auto)
        {
            readyToShoot = false;
        }

        if (activeGun.scatter)
        {
            // Shotgun - fire multiple pellets at once
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
            // Normal guns - fire bullets in sequence
            while (burstBulletsLeft > 0 && currentAmmo > 0 && !isReloading)
            {
                FireBullet();

                // Play sound for each bullet in burst/auto
                if (currentShootingMode == GunData.ShootingMode.Burst || currentShootingMode == GunData.ShootingMode.Auto)
                {
                    PlaySound(soundData?.GetShootClip(activeSound));
                }

                burstBulletsLeft--;

                if (currentShootingMode == GunData.ShootingMode.Single) break;

                yield return new WaitForSeconds(burstFireInterval);
            }
        }

        // Handle different weapon reset logic
        if (currentShootingMode == GunData.ShootingMode.Auto)
        {
            if (attackAction?.WasReleasedThisFrame() == true || currentAmmo <= 0)
            {
                ResetShot();
            }
            else
            {
                if (animator != null) animator.Play("Idle", 0, 0f);
            }
        }
        else
        {
            ResetShot();
        }
    }

    // Fire a single bullet
    private void FireBullet(bool ignoreAmmo = false)
    {
        if (!ignoreAmmo && currentAmmo <= 0) return;

        // Play effects
        SetAnimTrigger("RecoilHigh");
        muzzleEffect?.GetComponent<ParticleSystem>()?.Play();

        // Create and fire bullet
        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

        // Set bullet damage
        if (bullet.TryGetComponent(out Bullet bulletScript))
        {
            bulletScript.damage = activeGun.damage;
        }

        // Apply physics
        bullet.transform.forward = shootingDirection;
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb.AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);
        Destroy(bullet, bulletLifeTime);

        // Consume ammo
        if (!ignoreAmmo) currentAmmo--;
    }

    // Try to reload weapon
    private void TryReload()
    {
        if (!isReloading)
        {
            isReloading = true;
            SetAnimTrigger("Reload");
            PlaySound(soundData?.GetReloadClip(activeSound));
            StartCoroutine(ReloadWeapon(reloadTime));
        }
    }

    // Reload weapon over time
    private IEnumerator ReloadWeapon(float reloadTime)
    {
        yield return new WaitForSeconds(reloadTime);

        int missingAmmo = magazineCapacity - currentAmmo;

        if (totalAmmo <= 0)
        {
            // No ammo to reload
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

        SetAnimTrigger("ReloadRecover");
        isReloading = false;
    }

    // Reset shooting state
    private void ResetShot()
    {
        readyToShoot = true;
        SetAnimTrigger("RecoilRecover");

        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }
    }

    // Set weapon stats from gun data
    private void SetActiveGun(GunData.Attribute gun, SoundData.WeaponSound sound)
    {
        activeGun = gun;
        activeSound = sound;

        magazineCapacity = gun.magazineCapacity;
        // Only set starting ammo on first pickup
        if (!hasBeenPickedUpBefore)
        {
            currentAmmo = magazineCapacity;
            totalAmmo = gun.totalAmmo;
        }

        reloadTime = gun.reloadTime;
        fireRate = gun.fireRate;
        bulletVelocity = gun.bulletVelocity;
        bulletLifeTime = gun.bulletLifeTime;
        spreadIntensity = gun.spreadIntensity;
        bulletsPerBurst = Mathf.Max(1, gun.bulletsPerBurst);
        burstFireInterval = gun.burstFireInterval;

        availableModes = gun.availableModes?.Length > 0
            ? gun.availableModes
            : new GunData.ShootingMode[] { gun.shootingMode };

        currentModeIndex = 0;
        currentShootingMode = availableModes[currentModeIndex];
        burstBulletsLeft = bulletsPerBurst;
    }

    // Switch between available firing modes
    public void SwitchMode()
    {
        if (availableModes?.Length > 1)
        {
            currentModeIndex = (currentModeIndex + 1) % availableModes.Length;
            currentShootingMode = availableModes[currentModeIndex];
        }
    }

    // Calculate bullet direction with spread
    private Vector3 CalculateDirectionAndSpread()
    {
        // Get camera center ray
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        // Cast from camera to get target point
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100f);
        }

        // If we're close to the target, extend it to avoid weird angles
        float distanceToTarget = Vector3.Distance(Camera.main.transform.position, targetPoint);
        if (distanceToTarget < 5f)
        {
            targetPoint = ray.GetPoint(100f);
        }

        // Calculate direction from bullet spawn to target
        Vector3 direction = (targetPoint - bulletSpawn.position).normalized;

        // If aiming and weapon is offset, correct for parallax
        if (AimingManager.Instance != null && AimingManager.Instance.isAiming)
        {
            // Project the bullet spawn position onto the camera forward plane
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraPos = Camera.main.transform.position;
            Vector3 spawnOffset = bulletSpawn.position - cameraPos;

            // Remove the forward component of the offset
            float forwardDistance = Vector3.Dot(spawnOffset, cameraForward);
            Vector3 lateralOffset = spawnOffset - (cameraForward * forwardDistance);

            // Adjust target point by the lateral offset
            Vector3 adjustedTarget = targetPoint + lateralOffset;
            direction = (adjustedTarget - bulletSpawn.position).normalized;
        }

        // Apply accuracy with aiming multiplier
        float aimingAccuracy = 1f;
        if (AimingManager.Instance != null)
        {
            aimingAccuracy = AimingManager.Instance.GetAccuracyMultiplier();
        }

        float accuracyMultiplier = (2f - activeGun.accuracy) * 5f * aimingAccuracy;
        float finalSpread = spreadIntensity * accuracyMultiplier;

        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-finalSpread, finalSpread),
            Random.Range(-finalSpread, finalSpread),
            0
        );

        return spreadRotation * direction;
    }

    // Trigger animation safely
    private void SetAnimTrigger(string triggerName)
    {
        if (animator?.HasParameterOfType(triggerName, AnimatorControllerParameterType.Trigger) == true)
        {
            animator.SetTrigger(triggerName);
        }
    }

    // Play sound safely
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
