using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ReactorBreach.Environment
{
    [RequireComponent(typeof(Collider))]
    public class BreakableSurface : MonoBehaviour, IBreakable
    {
        [SerializeField] private float _breakThreshold = 500f;
        [SerializeField] private GameObject[] _fragmentObjects;  // pre-fractured children

        public float BreakThreshold => _breakThreshold;
        private bool _broken;

        private void OnCollisionEnter(Collision collision)
        {
            if (_broken) return;

            float impulse = collision.impulse.magnitude;
            if (impulse >= _breakThreshold)
                Break(collision.contacts[0].point, impulse);
        }

        public void Break(Vector3 impactPoint, float force)
        {
            if (_broken) return;
            _broken = true;

            // Disable main collider and renderer
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            if (TryGetComponent<Renderer>(out var rend)) rend.enabled = false;

            // Activate fragments
            if (_fragmentObjects != null)
            {
                foreach (var frag in _fragmentObjects)
                {
                    if (frag == null) continue;
                    frag.SetActive(true);
                    if (frag.TryGetComponent<Rigidbody>(out var rb))
                    {
                        Vector3 dir = (frag.transform.position - impactPoint).normalized;
                        rb.AddForce(dir * (force * 0.01f), ForceMode.Impulse);
                    }
                }
            }

            Enemies.VibrationSystem.Emit(transform.position, Data.GameConstants.VibrationRadiusImpact);

            // Self-destroy after delay
            Destroy(gameObject, 5f);
        }
    }
}
