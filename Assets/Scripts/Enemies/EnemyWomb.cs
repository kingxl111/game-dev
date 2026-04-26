using System.Collections;
using UnityEngine;
using ReactorBreach.Data;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Enemies
{
    /// <summary>
    /// Утроба — стационарный мини-босс. Спавнит Слизней раз в N секунд.
    /// Уязвима к раздавливанию и закупорке рта сваркой.
    /// </summary>
    public class EnemyWomb : EnemyBase
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private EnemyConfig _spawnedEnemyConfig;

        private bool _mouthBlocked;
        private Coroutine _spawnRoutine;

        protected override void Awake()
        {
            base.Awake();
            Agent.enabled = false; // неподвижен
        }

        private void Start()
        {
            if (Config != null && Config.SpawnInterval > 0f)
                _spawnRoutine = StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            while (!IsNeutralized)
            {
                yield return new WaitForSeconds(Config.SpawnInterval);

                if (_mouthBlocked || IsNeutralized) continue;

                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            if (_spawnedEnemyConfig?.Prefab == null) return;

            Vector3 pos = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position + transform.forward;

            Instantiate(_spawnedEnemyConfig.Prefab, pos, Quaternion.identity);
        }

        /// <summary>
        /// Вызывается когда WeldTool приваривает объект к «рту» Утробы.
        /// </summary>
        public void BlockMouth()
        {
            _mouthBlocked = true;
        }

        public void UnblockMouth()
        {
            _mouthBlocked = false;
        }

        protected override void Neutralize()
        {
            if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
            base.Neutralize();
        }
    }
}
