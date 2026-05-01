using UnityEngine;
using ReactorBreach.Player;

namespace ReactorBreach.Enemies
{
    /// <summary>«Глаз» разворачивается в сторону игрока, даже пока тело вращает NavMesh.</summary>
    public class EnemyAimEyes : MonoBehaviour
    {
        [SerializeField] private Transform _eye;
        [SerializeField] private Transform _faceRoot;
        [SerializeField] private float _slerpSpeed = 14f;
        [SerializeField] private float _aimHeightOffset = 0.4f;
        [SerializeField] private float _maxAimDistance = 64f;
        [SerializeField] private float _faceYawOffset = 0f;

        private void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Transform aimOrigin = _eye != null ? _eye : _faceRoot;
            if (aimOrigin == null) return;

            Vector3 target = player.transform.position + Vector3.up * _aimHeightOffset;
            Vector3 to = target - aimOrigin.position;
            if (to.sqrMagnitude < 0.01f || to.sqrMagnitude > _maxAimDistance * _maxAimDistance) return;

            var targetRot = Quaternion.LookRotation(to.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-_slerpSpeed * Time.deltaTime);
            if (_eye != null)
                _eye.rotation = Quaternion.Slerp(_eye.rotation, targetRot, t);

            if (_faceRoot != null)
            {
                Vector3 flat = player.transform.position - _faceRoot.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                {
                    var faceRot = Quaternion.LookRotation(flat.normalized, Vector3.up)
                                * Quaternion.Euler(0f, _faceYawOffset, 0f);
                    _faceRoot.rotation = Quaternion.Slerp(_faceRoot.rotation, faceRot, t);
                }
            }
        }
    }
}
