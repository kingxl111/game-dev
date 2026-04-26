using UnityEngine;

namespace ReactorBreach.UI
{
    /// <summary>
    /// Показывает подсказку один раз после загрузки уровня (хаб).
    /// </summary>
    public class LevelStartHint : MonoBehaviour
    {
        [SerializeField] private float _delaySeconds = 0.35f;
        [SerializeField] private string _title;
        [SerializeField] private string _body;

        private void Start()
        {
            Invoke(nameof(Show), _delaySeconds);
        }

        private void Show()
        {
            if (LevelHintPanel.Instance != null)
                LevelHintPanel.Instance.Show(_title, _body);
        }
    }
}
