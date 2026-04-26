using System.Collections.Generic;
using UnityEngine;

namespace ReactorBreach.Enemies
{
    public class VibrationSystem : MonoBehaviour
    {
        public static VibrationSystem Instance { get; private set; }

        private readonly List<EnemyBase> _registeredEnemies = new();

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Emit(Vector3 position, float radius)
        {
            if (Instance == null) return;

            foreach (var enemy in Instance._registeredEnemies)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(position, enemy.transform.position);
                if (dist <= radius)
                {
                    float intensity = 1f - dist / radius;
                    enemy.OnVibrationDetected(position, intensity);
                }
            }
        }

        public static void Register(EnemyBase enemy)
        {
            Instance?._registeredEnemies.Add(enemy);
        }

        public static void Unregister(EnemyBase enemy)
        {
            Instance?._registeredEnemies.Remove(enemy);
        }
    }
}
