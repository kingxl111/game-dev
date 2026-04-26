using UnityEngine;
using TMPro;
using ReactorBreach.Core;

namespace ReactorBreach.UI
{
    public class ObjectiveDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _objectiveText;

        private ObjectiveTracker _tracker;

        private void Start()
        {
            _tracker = FindFirstObjectByType<ObjectiveTracker>();
            if (_tracker != null)
                _tracker.OnObjectiveUpdated += SetText;
        }

        private void OnDestroy()
        {
            if (_tracker != null)
                _tracker.OnObjectiveUpdated -= SetText;
        }

        private void SetText(string text)
        {
            if (_objectiveText != null)
                _objectiveText.text = text;
        }
    }
}
