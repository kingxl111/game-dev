using UnityEngine;
using ReactorBreach.Data;

namespace ReactorBreach.UI
{
    [RequireComponent(typeof(Collider))]
    public class HintZoneTrigger : MonoBehaviour
    {
        [SerializeField] private string _title;
        [SerializeField] [TextArea(2, 8)] private string _body;
        [SerializeField] private string _dismissContext;
        [SerializeField] private bool _once = true;

        private bool _shown;

        private void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_once && _shown) return;
            if (!other.CompareTag(GameConstants.TagPlayer)) return;
            if (LevelHintPanel.Instance == null) return;
            if (string.IsNullOrEmpty(_dismissContext))
                LevelHintPanel.Instance.Show(_title, _body);
            else
                LevelHintPanel.Instance.Show(_title, _body, _dismissContext);
            _shown = true;
        }
    }
}
