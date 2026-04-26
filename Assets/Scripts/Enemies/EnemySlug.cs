using UnityEngine;
using ReactorBreach.ScriptableObjects;

namespace ReactorBreach.Enemies
{
    /// <summary>
    /// Слизень — основной рядовой враг. HP=50, Speed=3, поведение через FSM.
    /// </summary>
    public class EnemySlug : EnemyBase
    {
        protected override void Awake()
        {
            base.Awake();
        }
    }
}
