using UnityEngine;
using ReactorBreach.UI;

namespace ReactorBreach.Enemies
{
    [DisallowMultipleComponent]
    public class EnemyVisionGatedByHint : MonoBehaviour
    {
        [SerializeField] private string _requiredDismissContext = "ZoneB";

        public bool IsUnlocked { get; private set; }

        private void Start()
        {
            if (string.IsNullOrEmpty(_requiredDismissContext)) return;
            if (LevelHintPanel.WasHintContextDismissed(_requiredDismissContext))
                IsUnlocked = true;
        }

        private void OnEnable()
        {
            LevelHintPanel.OnHintDismissed += OnHint;
            if (!string.IsNullOrEmpty(_requiredDismissContext) && LevelHintPanel.WasHintContextDismissed(_requiredDismissContext))
                IsUnlocked = true;
        }

        private void OnDisable()
        {
            LevelHintPanel.OnHintDismissed -= OnHint;
        }

        private void OnHint(string ctx)
        {
            if (IsUnlocked) return;
            if (string.IsNullOrEmpty(_requiredDismissContext)) return;
            if (ctx == _requiredDismissContext) IsUnlocked = true;
        }
    }
}
