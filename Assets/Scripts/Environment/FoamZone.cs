using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReactorBreach.Environment
{
    [RequireComponent(typeof(SphereCollider))]
    public class FoamZone : MonoBehaviour
    {
        // §3.4 — стакающиеся зоны и «бугорки»
        private const float StackOverlapRadius = 1f;
        private const int   MaxBumpsPerPoint   = 3;
        private const float BumpRadius         = 0.4f;

        private static readonly List<FoamZone> AliveZones = new();
        private static readonly List<GameObject> BumpsAtPoint = new();

        private float _radius;
        private float _duration;
        private float _slowMultiplier;
        private float _stickThreshold;

        private readonly List<Rigidbody> _stuckObjects = new();
        private SphereCollider _trigger;
        private bool _initialized;

        public void Init(float radius, float duration, float slowMultiplier,
                         float stickThreshold, Vector3 surfaceNormal)
        {
            _radius         = radius;
            _duration       = duration;
            _slowMultiplier = slowMultiplier;
            _stickThreshold = stickThreshold;
            _initialized    = true;

            _trigger          = GetComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius   = radius;

            // Align slightly above surface
            transform.up = surfaceNormal;

            TrySpawnBump(surfaceNormal);
            AliveZones.Add(this);

            StartCoroutine(ExpireRoutine());
        }

        /// <summary>
        /// §3.4 — если в радиусе 1м уже есть ≥1 зона, создаётся бугорок (kinematic SphereCollider).
        /// Максимум 3 бугорка в одной точке.
        /// </summary>
        private void TrySpawnBump(Vector3 normal)
        {
            int neighbours = 0;
            foreach (var z in AliveZones)
            {
                if (z == null || z == this) continue;
                if (Vector3.Distance(z.transform.position, transform.position) <= StackOverlapRadius)
                    neighbours++;
            }
            if (neighbours < 1) return;

            // Считаем сколько бугорков уже рядом
            BumpsAtPoint.Clear();
            int existing = 0;
            var hits = Physics.OverlapSphere(transform.position, StackOverlapRadius);
            foreach (var h in hits)
                if (h.CompareTag("FoamBump")) existing++;
            if (existing >= MaxBumpsPerPoint) return;

            var bump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bump.tag = "FoamBump";
            bump.name = "FoamBump";
            bump.transform.position = transform.position + normal.normalized * BumpRadius * 0.5f;
            bump.transform.localScale = Vector3.one * BumpRadius * 2f;

            var rb = bump.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void Awake()
        {
            _trigger = GetComponent<SphereCollider>();
            if (_trigger == null)
            {
                _trigger = gameObject.AddComponent<SphereCollider>();
                _trigger.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_initialized) return;

            var enemy = other.GetComponentInParent<Enemies.EnemyBase>();
            if (enemy != null)
                enemy.ApplySlow(_slowMultiplier, _duration);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_initialized) return;

            // Пока враг в пене — обновляем замедление (иначе срок кончался, а враг ещё в зоне)
            var enStay = other.GetComponentInParent<Enemies.EnemyBase>();
            if (enStay != null)
                enStay.ApplySlow(_slowMultiplier, _duration);

            // Stick slow-moving physics objects (not player)
            if (other.CompareTag(Data.GameConstants.TagPlayer)) return;

            if (other.TryGetComponent<Rigidbody>(out var rb) && !rb.isKinematic)
            {
                if (rb.linearVelocity.magnitude < _stickThreshold)
                {
                    rb.isKinematic = true;
                    _stuckObjects.Add(rb);

                    var enemyStick = other.GetComponentInParent<Enemies.EnemyBase>();
                    if (enemyStick != null)
                        enemyStick.SetStuck(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var exitRb = other.GetComponent<Rigidbody>();
            var enemy = other.GetComponentInParent<Enemies.EnemyBase>();
            if (enemy != null && (exitRb == null || !_stuckObjects.Contains(exitRb)))
                enemy.ApplySlow(1f, 0f);
        }

        private IEnumerator ExpireRoutine()
        {
            yield return new WaitForSeconds(_duration);

            // Trigger collider off — stuck objects remain (§19.2: затвердевание)
            _trigger.enabled = false;

            // Per ТЗ 19.2: stuck objects become permanent environment
            foreach (var rb in _stuckObjects)
            {
                if (rb == null) continue;
                rb.isKinematic = true;
                rb.gameObject.layer = Data.GameConstants.LayerStatic;
            }

            AliveZones.Remove(this);

            // Destroy visual only; stuck objects stay
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            AliveZones.Remove(this);
        }
    }
}
