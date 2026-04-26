using UnityEngine;
using ReactorBreach.Data;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Газовый баллон. Взрывается при получении CrushDamage > 100.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(WeldableObject))]
    [RequireComponent(typeof(GravityAffectableObject))]
    public class GasCanister : MonoBehaviour
    {
        [SerializeField] private float _explosionThreshold = 100f;
        [SerializeField] private float _explosionRadius    = 5f;
        [SerializeField] private float _explosionForce     = 3000f;
        [SerializeField] private GameObject _explosionVFX;

        private bool _exploded;

        private void OnCollisionEnter(Collision collision)
        {
            if (_exploded) return;

            float impulse = collision.impulse.magnitude;
            var rb = collision.rigidbody;
            float crushDamage = rb != null
                ? rb.mass * collision.relativeVelocity.magnitude * 0.5f
                : impulse * 0.1f;

            if (crushDamage > _explosionThreshold)
                Explode(transform.position);
        }

        private void Explode(Vector3 point)
        {
            _exploded = true;

            if (_explosionVFX != null)
                Instantiate(_explosionVFX, point, Quaternion.identity);

            // Physics blast
            var colliders = Physics.OverlapSphere(point, _explosionRadius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<Rigidbody>(out var rb))
                    rb.AddExplosionForce(_explosionForce, point, _explosionRadius, 1f, ForceMode.Impulse);

                if (col.TryGetComponent<Enemies.EnemyBase>(out var enemy))
                    enemy.TakeDamage(200f, DamageType.Crush);

                if (col.TryGetComponent<Player.PlayerHealth>(out var health))
                    health.TakeDamage(40f);

                if (col.TryGetComponent<IBreakable>(out var breakable))
                    breakable.Break(point, _explosionForce);
            }

            Enemies.VibrationSystem.Emit(point, _explosionRadius * 2f);
            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        }
    }
}
