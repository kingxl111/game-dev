using System;
using UnityEngine;

namespace ReactorBreach.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float _maxHP = 100f;
        [SerializeField] private float _regenRate = 2f;
        [SerializeField] private float _regenDelay = 5f;

        public float CurrentHP { get; private set; }
        public float MaxHP => _maxHP;
        public bool IsDead => _isDead;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        private float _lastDamageTime = float.NegativeInfinity;
        private bool _isDead;

        private void Awake()
        {
            CurrentHP = _maxHP;
        }

        private void Update()
        {
            if (_isDead) return;

            if (CurrentHP < _maxHP && Time.time - _lastDamageTime > _regenDelay)
            {
                CurrentHP = Mathf.Min(_maxHP, CurrentHP + _regenRate * Time.deltaTime);
                OnHealthChanged?.Invoke(CurrentHP, _maxHP);
            }
        }

        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            CurrentHP = Mathf.Max(0f, CurrentHP - amount);
            _lastDamageTime = Time.time;
            OnHealthChanged?.Invoke(CurrentHP, _maxHP);

            if (CurrentHP <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (_isDead) return;
            CurrentHP = Mathf.Min(_maxHP, CurrentHP + amount);
            OnHealthChanged?.Invoke(CurrentHP, _maxHP);
        }

        private void Die()
        {
            _isDead = true;
            OnDeath?.Invoke();
            Core.GameManager.Instance?.GameOver();
        }
    }
}
