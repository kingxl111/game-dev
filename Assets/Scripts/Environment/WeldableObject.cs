using System;
using System.Collections.Generic;
using UnityEngine;
using ReactorBreach.Tools;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Компонент физического объекта, к которому можно применить сварку.
    /// Вешается на ящики, трубы, панели, стены, а также на Enemy.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeldableObject : MonoBehaviour, IWeldable
    {
        [SerializeField] private bool _canBeWelded = true;

        private Rigidbody _rb;
        private readonly List<WeldConnection> _activeConnections = new();

        public Rigidbody WeldRigidbody => _rb;
        public Transform WeldTransform => transform;
        public bool CanBeWelded        => _canBeWelded && _activeConnections.Count < 4;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void OnWelded(WeldConnection connection)
        {
            if (this == null) return;
            _activeConnections.Add(connection);

            if (TryGetComponent<Enemies.EnemyBase>(out var enemy))
            {
                enemy.WeldedTimer = 0.001f;
                enemy.SetStuck(true);
            }
        }

        public void OnWeldBroken(WeldConnection connection)
        {
            if (this == null) return;
            _activeConnections.Remove(connection);

            if (_activeConnections.Count == 0)
            {
                // Без TryGetComponent: на уничтожающемся компоненте он может кинуть MissingReference.
                var enemy = GetComponent<Enemies.EnemyBase>();
                if (enemy != null)
                {
                    enemy.WeldedTimer = 0f;
                    enemy.SetStuck(false);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            float impulse = collision.impulse.magnitude;
            if (impulse > Data.GameConstants.ImpactVibrationThreshold)
                Enemies.VibrationSystem.Emit(transform.position, Data.GameConstants.VibrationRadiusImpact);
        }
    }
}
