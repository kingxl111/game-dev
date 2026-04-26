using UnityEngine;
using ReactorBreach.Player;

namespace ReactorBreach.Enemies
{
    /// <summary>«Глаз» разворачивается в сторону игрока, даже пока тело вращает NavMesh.</summary>
    public class EnemyAimEyes : MonoBehaviour
    {
        [SerializeField] private Transform _eye;
        [SerializeField] private float _slerpSpeed = 14f;
        [SerializeField] private float _aimHeightOffset = 0.4f;
        [SerializeField] private float _maxAimDistance = 64f;

        private void LateUpdate()
        {
            if (_eye == null) return;
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 target = player.transform.position + Vector3.up * _aimHeightOffset;
            Vector3 to = target - _eye.position;
            if (to.sqrMagnitude < 0.01f || to.sqrMagnitude > _maxAimDistance * _maxAimDistance) return;

            var targetRot = Quaternion.LookRotation(to.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-_slerpSpeed * Time.deltaTime);
            _eye.rotation = Quaternion.Slerp(_eye.rotation, targetRot, t);
        }
    }
}
