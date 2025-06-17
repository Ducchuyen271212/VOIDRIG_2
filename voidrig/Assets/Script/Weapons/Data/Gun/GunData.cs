//GunData.cs
using System;
using UnityEngine;

[Serializable]
public class GunData : MonoBehaviour
{
    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    [Serializable]
    public class Attribute
    {
        public int magazineCapacity;
        public int totalAmmo;

        public float damage;
        public float bulletVelocity;
        public float knockBack;
        public float bulletLifeTime;
        public float accuracy;
        public float fireRate;
        public float reloadTime;

        public bool scatter;
        public float spreadIntensity;
        public int bulletsPerBurst;
        public float burstFireInterval;

        public float overheatThreshold; // Added

        public ShootingMode shootingMode;
        public ShootingMode[] availableModes;
    }

    public Attribute machineGun = new Attribute
    {
        magazineCapacity = 240,
        totalAmmo = 1200,
        damage = 3f,
        bulletVelocity = 180f,
        knockBack = 2f,
        bulletLifeTime = 2f,
        accuracy = 0.7f,
        fireRate = 0.05f,
        reloadTime = 1f,
        scatter = false,
        spreadIntensity = 0.4f,
        bulletsPerBurst = 1,
        burstFireInterval = 0.05f,
        overheatThreshold = 600f,
        shootingMode = ShootingMode.Auto,
        availableModes = new ShootingMode[]
        { ShootingMode.Auto, ShootingMode.Burst, ShootingMode.Single }
    };

    public Attribute shotGun = new Attribute
    {
        magazineCapacity = 7,
        totalAmmo = 28,
        damage = 1f,
        bulletVelocity = 180f,
        knockBack = 10f,
        bulletLifeTime = 0.5f,
        accuracy = 0.3f,
        fireRate = 0.5f,
        reloadTime = 1f,
        scatter = true,
        spreadIntensity = 8f,
        bulletsPerBurst = 40,
        burstFireInterval = 0.15f,
        overheatThreshold = 400f,
        shootingMode = ShootingMode.Burst,
        availableModes = new ShootingMode[]
        { ShootingMode.Burst }
    };

    public Attribute sniper = new Attribute
    {
        magazineCapacity = 5,
        totalAmmo = 20,
        damage = 10f,
        bulletVelocity = 540f,
        knockBack = 1f,
        bulletLifeTime = 3f,
        accuracy = 1f,
        fireRate = 1f,
        reloadTime = 2f,
        scatter = false,
        spreadIntensity = 0f,
        bulletsPerBurst = 1,
        burstFireInterval = 0.5f,
        overheatThreshold = 10f,
        shootingMode = ShootingMode.Single,
        availableModes = new ShootingMode[]
        { ShootingMode.Single }
    };

    public Attribute handGun = new Attribute
    {
        magazineCapacity = 10,
        totalAmmo = 40,
        damage = 4f,
        bulletVelocity = 180f,
        knockBack = 4f,
        bulletLifeTime = 2f,
        accuracy = 0.85f,
        fireRate = 0.1f,
        reloadTime = 1f,
        scatter = false,
        spreadIntensity = 0.1f,
        bulletsPerBurst = 1,
        burstFireInterval = 0.15f,
        overheatThreshold = 100f,
        shootingMode = ShootingMode.Single,
        availableModes = new ShootingMode[]
        { ShootingMode.Single, ShootingMode.Auto }
    };

    public Attribute smg = new Attribute
    {
        magazineCapacity = 45,
        totalAmmo = 270,
        damage = 5f,
        bulletVelocity = 160f,
        knockBack = 1f,
        bulletLifeTime = 1.5f,
        accuracy = 0.6f,
        fireRate = 0.07f,
        reloadTime = 1.2f,
        scatter = false,
        spreadIntensity = 0.6f,
        bulletsPerBurst = 1,
        burstFireInterval = 0.05f,
        overheatThreshold = 600f,
        shootingMode = ShootingMode.Auto,
        availableModes = new ShootingMode[]
        { ShootingMode.Auto, ShootingMode.Burst }
    };

    public Attribute burstRifle = new Attribute
    {
        magazineCapacity = 30,
        totalAmmo = 180,
        damage = 5f,
        bulletVelocity = 220f,
        knockBack = 2f,
        bulletLifeTime = 2f,
        accuracy = 0.9f,
        fireRate = 0.3f,
        reloadTime = 1.5f,
        scatter = false,
        spreadIntensity = 0.15f,
        bulletsPerBurst = 3,
        burstFireInterval = 0.1f,
        overheatThreshold = 600f,
        shootingMode = ShootingMode.Burst,
        availableModes = new ShootingMode[]
        { ShootingMode.Burst, ShootingMode.Single }
    };
}