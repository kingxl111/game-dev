using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ReactorBreach.Data;
using ReactorBreach.UI;

namespace ReactorBreach.Environment
{
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private float _activationMass = 200f;
        [SerializeField] private UnityEvent _onActivated;
        [SerializeField] private UnityEvent _onDeactivated;

        [SerializeField] private LayerMask _queryLayers = (1 << 0) | (1 << GameConstants.LayerInteractable);

        [SerializeField] private int _overlapBufferSize = 32;

        private bool _isActive;
        private Collider[] _overlapBuffer;
        private readonly HashSet<int> _seenRigidbodies = new();

        private void Awake()
        {
            if ((int)_queryLayers == 0)
                _queryLayers = (1 << 0) | (1 << GameConstants.LayerInteractable);
            if (_overlapBufferSize < 8) _overlapBufferSize = 8;
            _overlapBuffer = new Collider[_overlapBufferSize];
        }

        /// <summary>
        /// Сумма масс считается по физ. запросу в объёме плиты каждый FixedUpdate: не полагаемся
        /// на OnTrigger* (с матрицей слоёв, «спящими» и сменой mass без движения).
        /// </summary>
        private void FixedUpdate()
        {
            Collider selfCol = GetComponent<Collider>();
            if (selfCol == null) return;

            Vector3 center, halfExt;
            Quaternion rot;
            if (selfCol is BoxCollider box)
            {
                var t = box.transform;
                center  = t.TransformPoint(box.center);
                halfExt = Vector3.Scale(box.size, t.lossyScale) * 0.5f;
                rot     = t.rotation;
            }
            else
            {
                Bounds b = selfCol.bounds;
                center  = b.center;
                halfExt = b.extents;
                rot     = Quaternion.identity;
            }

            if (halfExt.y < 0.6f)
            {
                center.y += 0.35f;
                halfExt.y = 0.6f;
            }

            int n = Physics.OverlapBoxNonAlloc(
                center, halfExt, _overlapBuffer, rot, _queryLayers, QueryTriggerInteraction.Ignore);

            _seenRigidbodies.Clear();
            float totalMass = 0f;
            for (int i = 0; i < n; i++)
            {
                var c = _overlapBuffer[i];
                if (c == null || c == selfCol) continue;
                Rigidbody rb = c.GetComponentInParent<Rigidbody>();
                if (rb == null) continue;
                if (!_seenRigidbodies.Add(rb.GetInstanceID())) continue;
                totalMass += rb.mass;
            }

            CheckActivation(totalMass);
        }

        private void CheckActivation(float totalMass)
        {
            bool shouldBeActive = totalMass >= _activationMass;
            if (shouldBeActive == _isActive) return;

            _isActive = shouldBeActive;
            if (_isActive)
            {
                _onActivated?.Invoke();
                HUDController.ShowMechanicNoteStatic("Плита: достаточная масса", 2.2f);
            }
            else
                _onDeactivated?.Invoke();
        }
    }
}
