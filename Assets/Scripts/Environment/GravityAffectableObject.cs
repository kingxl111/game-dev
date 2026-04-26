using UnityEngine;
using ReactorBreach.Data;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Компонент для объектов с Rigidbody, поддерживающих IGravityAffectable.
    /// Вешается вместе с WeldableObject.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GravityAffectableObject : MonoBehaviour, IGravityAffectable
    {
        private Rigidbody _rb;
        private bool _isAffected;
        private float _originalMass;
        private float _spawnMass;

        public Rigidbody TargetRigidbody => _rb;
        public float OriginalMass        => _originalMass;
        public bool IsAffected           => _isAffected;

        /// <summary>Толкание только после «облегчения» (текущая масса строго меньше стартовой с уровня).</summary>
        public bool CanBePushedByPlayer => _rb != null && _spawnMass > 0.0001f && _rb.mass < _spawnMass * 0.99f;

        private void Awake()
        {
            _rb           = GetComponent<Rigidbody>();
            _originalMass = _rb.mass;
            _spawnMass    = _rb.mass;
        }

        public void ApplyGravityEffect(float multiplier, float duration)
        {
            if (_isAffected) return;
            _isAffected = true;
            _originalMass = _rb.mass;
            _rb.mass *= multiplier;

            StartCoroutine(RestoreAfter(duration));
        }

        public void ApplyLightGravityEffect(float divider, float duration)
        {
            if (_isAffected) return;
            if (divider < 0.0001f) divider = 30f;
            _isAffected = true;
            _originalMass = _rb.mass;
            _rb.mass = Mathf.Max(0.01f, _rb.mass / divider);

            StartCoroutine(RestoreAfter(duration));
        }

        private System.Collections.IEnumerator RestoreAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            _rb.mass    = _originalMass;
            _isAffected = false;
        }
    }
}
