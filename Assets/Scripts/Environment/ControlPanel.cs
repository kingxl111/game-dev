using UnityEngine;
using ReactorBreach.Core;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Триггер «пульта управления» (§2.1). При входе игрока в зону —
    /// уведомляет ObjectiveTracker о выполнении цели Reach.
    /// Также используется как точка активации клапана для босса (§4.4).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ControlPanel : MonoBehaviour
    {
        [SerializeField] private bool _isValve;
        [SerializeField] private Enemies.EnemyCore _coreReference;

        private bool _activated;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_activated) return;
            if (!other.CompareTag(Data.GameConstants.TagPlayer)) return;

            _activated = true;

            if (_isValve && _coreReference != null)
            {
                _coreReference.OnValveActivated();
                return;
            }

            var tracker = FindAnyObjectByType<ObjectiveTracker>();
            tracker?.NotifyPlayerReached();
        }
    }
}
