using UnityEngine;

namespace ReactorBreach.Environment
{
    /// <summary>
    /// Маркер щупальца босса «Ядро» (§4.4). Вешается на отдельный GameObject-щупальце
    /// рядом с EnemyCore. WeldTool вызывает OnWelded() при приваривании,
    /// который сообщает ссылке на босса об индексе зафиксированного щупальца.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(WeldableObject))]
    public class CoreTentacle : MonoBehaviour
    {
        [SerializeField] private Enemies.EnemyCore _core;
        [SerializeField] private int _tentacleIndex;

        private bool _welded;

        public bool IsWelded => _welded;

        public void OnWelded()
        {
            if (_welded || _core == null) return;
            _welded = true;
            _core.OnTentacleWelded(_tentacleIndex);
        }
    }
}
