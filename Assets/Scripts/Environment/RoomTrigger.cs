using UnityEngine;
using UnityEngine.Events;
using ReactorBreach.Data;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Универсальный триггер «игрок вошёл в зону» с UnityEvent.
    /// Используется для активации дверей, чек-поинтов, инструкций и т. п.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomTrigger : MonoBehaviour
    {
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private UnityEvent _onPlayerEntered;

        private bool _triggered;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered && _triggerOnce) return;
            if (!other.CompareTag(GameConstants.TagPlayer)) return;

            _triggered = true;
            _onPlayerEntered?.Invoke();
        }
    }
}
