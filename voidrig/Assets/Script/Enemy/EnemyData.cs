using System;
using UnityEngine;

public class EnemyData : MonoBehaviour
{
    [Serializable]

    public enum healthType
    {
        
    }

    public class Attribute
    {
        public int health;
        public float speed;
        public float attackRange;
        public float attackDamage;
        public float attackCooldown;
        public float detectionRange;
        public float knockBackResistance;
        // Additional attributes can be added here
    }
}
