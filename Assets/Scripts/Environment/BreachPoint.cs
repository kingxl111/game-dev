using System;
using UnityEngine;
using ReactorBreach.Data;

namespace ReactorBreach.Environment
{
    public class BreachPoint : MonoBehaviour
    {
        [SerializeField] private float _suctionForce    = 20f;
        [SerializeField] private float _suctionRadius   = 5f;
        [SerializeField] private float _damagePerSecond = 10f;

        [SerializeField] private GameObject _breachVFX;
        [SerializeField] private GameObject _sealedVFX;
        [SerializeField] private AudioSource _windAudio;

        public bool IsSealed { get; private set; }
        public event Action OnSealed;

        private void FixedUpdate()
        {
            if (IsSealed) return;

            var colliders = Physics.OverlapSphere(transform.position, _suctionRadius);
            foreach (var col in colliders)
            {
                // Suction on physics objects
                if (col.TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
                {
                    var dir = (transform.position - rb.position).normalized;
                    rb.AddForce(dir * _suctionForce, ForceMode.Force);
                }

                // Damage to player
                if (col.TryGetComponent<Player.PlayerHealth>(out var health))
                    health.TakeDamage(_damagePerSecond * Time.fixedDeltaTime);

                // Damage to enemies
                if (col.TryGetComponent<Enemies.EnemyBase>(out var enemy))
                    enemy.TakeDamage(_damagePerSecond * Time.fixedDeltaTime, DamageType.Decompression);
            }
        }

        /// <summary>
        /// Вызывается когда объект (панель/ящик) приварен к краям пробоины.
        /// Логику вызова обеспечивает WeldTool при обнаружении BreachPoint среди целей.
        /// </summary>
        public void Seal()
        {
            if (IsSealed) return;
            IsSealed = true;

            if (_breachVFX != null) _breachVFX.SetActive(false);
            if (_sealedVFX != null) _sealedVFX.SetActive(true);
            if (_windAudio != null) _windAudio.Stop();

            OnSealed?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _suctionRadius);
        }
    }
}
