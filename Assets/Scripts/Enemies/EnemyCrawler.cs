using UnityEngine;
using UnityEngine.AI;

namespace ReactorBreach.Enemies
{
    /// <summary>
    /// Ползун — движется по стенам и потолку через дополнительный NavMeshAgent.
    /// HP=30, Speed=5, AttackDamage=20.
    /// Переключение между обычным и wall NavMesh — через Off-Mesh Links в сцене.
    /// </summary>
    public class EnemyCrawler : EnemyBase
    {
        [SerializeField] private bool _isOnWall;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        private void LateUpdate()
        {
            if (_isOnWall)
                AlignToSurface();
        }

        private void AlignToSurface()
        {
            if (Physics.Raycast(transform.position, -transform.up, out var hit, 0.5f))
            {
                transform.up = hit.normal;
            }
        }
    }
}
