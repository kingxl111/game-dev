using UnityEngine;

namespace ReactorBreach.ScriptableObjects
{
    [CreateAssetMenu(menuName = "ReactorBreach/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public string EnemyName;
        public float  MaxHP          = 50f;
        public float  MoveSpeed      = 3f;
        public float  AttackDamage   = 15f;
        public float  AttackCooldown = 2f;
        public float  DetectionRange = 15f;
        public float  AttackRange    = 1.5f;
        public float  LoseTargetTime = 5f;
        public GameObject Prefab;

        [Header("Special")]
        public bool CanClimbWalls  = false;
        public bool IsStationary   = false;
        public float SpawnInterval = 0f;
        public EnemyConfig SpawnedEnemyType;
    }
}
