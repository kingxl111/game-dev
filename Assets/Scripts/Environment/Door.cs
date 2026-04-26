using UnityEngine;
using UnityEngine.Events;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Простая дверь: открывается/закрывается через UnityEvent или напрямую.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [SerializeField] private float _openAngle   = 90f;
        [SerializeField] private float _openSpeed   = 2f;
        [SerializeField] private bool  _openOnStart = false;
        [SerializeField] private UnityEvent _onOpened;
        [SerializeField] private UnityEvent _onClosed;

        private bool _isOpen;
        private Quaternion _closedRot;
        private Quaternion _openRot;

        private void Awake()
        {
            _closedRot = transform.localRotation;
            _openRot   = Quaternion.Euler(0f, _openAngle, 0f) * _closedRot;
        }

        private void Start()
        {
            if (_openOnStart) Open();
        }

        private void Update()
        {
            Quaternion target = _isOpen ? _openRot : _closedRot;
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, target, Time.deltaTime * _openSpeed);
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            _onOpened?.Invoke();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _onClosed?.Invoke();
        }

        public void Toggle() { if (_isOpen) Close(); else Open(); }
    }
}
