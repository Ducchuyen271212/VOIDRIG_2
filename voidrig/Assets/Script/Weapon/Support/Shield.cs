//Shield.cs
using UnityEngine;

namespace Weapons.Support
{
    public class Shield : MonoBehaviour
    {
        public ShieldType shieldType;
        public float shieldStrength = 100f;
        public float maxShieldStrength = 100f;

        public bool CanBlock(DamageInfo damageInfo)
        {
            if (shieldStrength <= 0) return false;

            ShieldBypassType requiredBypass = shieldType == ShieldType.Physical
                ? ShieldBypassType.PhysicalShield
                : ShieldBypassType.EnergyShield;

            return !damageInfo.CanBypass(requiredBypass);
        }
    }

    public enum ShieldType
    {
        Physical,
        Energy
    }
}

//end