using UnityEngine;
using ReactorBreach.Data;

namespace ReactorBreach.ScriptableObjects
{
    [CreateAssetMenu(menuName = "ReactorBreach/Level Objective")]
    public class LevelObjective : ScriptableObject
    {
        public string LevelName;
        public string Description;
        public ObjectiveType Type;
        public string SceneName;

        [Tooltip("Секунды до падения давления до нуля. 0 = без таймера.")]
        public float PressureTimer = 0f;

        public int EnemyCount  = 0;
        public int BreachCount = 0;

        [TextArea(3, 5)]
        public string BriefingText;
    }
}
