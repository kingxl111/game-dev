using System.Collections;
using UnityEngine;
using ReactorBreach.Data;
using ReactorBreach.Environment;

namespace ReactorBreach.Enemies
{
    /// <summary>
    /// Босс «Ядро» (уровень 5). Три фазы боя.
    /// Уничтожается активацией 4 клапанов, а не напрямую.
    /// </summary>
    public class EnemyCore : EnemyBase
    {
        [SerializeField] private Transform[] _tentacles;         // 4 щупальца
        [SerializeField] private GameObject[] _valves;           // 4 клапана
        [SerializeField] private int _valvesRequired = 4;

        private int _activatedValves;
        private bool _tentaclesPhase = true;
        private int _weldedTentacles;

        public bool IsTentaclesPhase => _tentaclesPhase;

        protected override void Awake()
        {
            base.Awake();
            Agent.enabled = false;
        }

        /// <summary>
        /// Вызывается ObjectiveTracker или ControlPanel когда игрок активировал клапан.
        /// </summary>
        public void OnValveActivated()
        {
            _activatedValves++;

            if (_activatedValves >= _valvesRequired)
                StartCoroutine(FinalSequence());
        }

        /// <summary>
        /// Вызывается когда WeldTool фиксирует щупальце.
        /// </summary>
        public void OnTentacleWelded(int index)
        {
            if (index >= 0 && index < _tentacles.Length && _tentacles[index] != null)
            {
                _tentacles[index].gameObject.SetActive(false);
                _weldedTentacles++;
                if (_weldedTentacles >= _tentacles.Length)
                    _tentaclesPhase = false;
            }
        }

        private IEnumerator FinalSequence()
        {
            // All valves open — reactor overloads — boss destroyed
            yield return new WaitForSeconds(3f);
            Neutralize();
        }

        protected override void Neutralize()
        {
            // Cannot be killed by physical damage, only by valve sequence
            if (_activatedValves < _valvesRequired) return;
            base.Neutralize();
        }

        public override void TakeDamage(float damage, DamageType type)
        {
            // Core is immune to all damage — only valve activation destroys it
        }
    }
}
