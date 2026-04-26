using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ReactorBreach.Core;
using ReactorBreach.Data;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyBase : MonoBehaviour, IEnemy
    {
        [SerializeField] protected EnemyConfig Config;

        protected NavMeshAgent Agent;
        protected EnemyStateMachine FSM;

        private float _currentHP;
        private bool _isNeutralized;
        private float _slowEndTime;
        private bool _isStuck;

        // Для FSM — таймер нейтрализации через сварку
        public float WeldedTimer { get; set; }

        public float CurrentHP    => _currentHP;
        public bool IsNeutralized => _isNeutralized;
        public bool IsStuck       => _isStuck;

        public float DetectionRange => Config != null ? Config.DetectionRange : 15f;
        public float AttackDamage   => Config != null ? Config.AttackDamage : 15f;
        public float AttackCooldown => Config != null ? Config.AttackCooldown : 2f;
        public float AttackRange    => Config != null ? Config.AttackRange : 1.5f;
        public float LoseTargetTime => Config != null ? Config.LoseTargetTime : 5f;

        public event Action OnNeutralized;

        protected virtual void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            FSM   = GetComponent<EnemyStateMachine>();

            if (Config != null)
            {
                _currentHP       = Config.MaxHP;
                Agent.speed      = Config.MoveSpeed;
                Agent.stoppingDistance = Config.AttackRange * 0.9f;
            }

            VibrationSystem.Register(this);
        }

        protected virtual void OnDestroy()
        {
            VibrationSystem.Unregister(this);
        }

        // ── IEnemy ────────────────────────────────────────────────────────

        public virtual void TakeDamage(float damage, DamageType type)
        {
            if (_isNeutralized) return;

            _currentHP -= damage;

            if (_currentHP <= 0f) Neutralize();
        }

        public void ApplySlow(float multiplier, float duration)
        {
            if (Config == null) return;
            Agent.speed = Config.MoveSpeed * multiplier;
            if (duration > 0.001f)
                _slowEndTime = Time.time + duration;
            else
            {
                _slowEndTime = 0f;
                if (multiplier >= 0.99f) Agent.speed = Config.MoveSpeed;
            }
        }

        public void SetStuck(bool stuck)
        {
            _isStuck = stuck;
            if (Agent != null)
                Agent.isStopped = stuck;

            if (stuck)
                FSM?.TransitionTo<AI.States.StuckState>();
        }

        // ── Vibration ────────────────────────────────────────────────────

        public virtual void OnVibrationDetected(Vector3 source, float intensity)
        {
            FSM?.OnVibrationDetected(source, intensity);
        }

        // ── Internal ──────────────────────────────────────────────────────

        protected virtual void Neutralize()
        {
            _isNeutralized = true;
            Agent.isStopped = true;
            Agent.enabled   = false;
            OnNeutralized?.Invoke();

            // Анимация смерти и уничтожение через 3 секунды
            StartCoroutine(DestroyAfterDelay(3f));
        }

        public void CheckWeldNeutralization()
        {
            if (WeldedTimer >= 5f && !_isNeutralized)
                Neutralize();
        }

        public void ForceNeutralize()
        {
            if (!_isNeutralized) Neutralize();
        }

        private IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        private void Update()
        {
            if (_isNeutralized) return;

            // Restore speed after slow expires
            if (_slowEndTime > 0f && Time.time >= _slowEndTime && Config != null)
            {
                Agent.speed  = Config.MoveSpeed;
                _slowEndTime = 0f;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            var pc = Player.PlayerController.Instance;
            if (pc == null) return;
            if (!pc.TryGetComponent<Player.PlayerHealth>(out var hp) || hp.IsDead) return;
            var enemyCollider = GetComponent<Collider>();
            var playerCollider = pc.GetComponent<CharacterController>();
            if (enemyCollider == null || playerCollider == null) return;
            if (Physics.ComputePenetration(
                    enemyCollider, transform.position, transform.rotation,
                    playerCollider, pc.transform.position, pc.transform.rotation,
                    out _, out _))
                hp.TakeDamage(5000f);
        }

        // ── Physics damage ────────────────────────────────────────────────

        private void OnCollisionEnter(Collision collision)
        {
            if (_isNeutralized) return;

            float impulse = collision.impulse.magnitude;
            if (impulse < 1f) return;

            // Crush from heavy object
            var rb = collision.rigidbody;
            if (rb != null)
            {
                float damage = rb.mass * collision.relativeVelocity.magnitude * 0.5f;
                TakeDamage(damage, DamageType.Crush);

                // Vibration from impact
                if (impulse > Data.GameConstants.ImpactVibrationThreshold)
                    VibrationSystem.Emit(transform.position, Data.GameConstants.VibrationRadiusImpact);
            }
        }
    }
}
