using UnityEngine;
using UnityEngine.UI;
using ReactorBreach.Player;

namespace ReactorBreach.UI
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private Image  _fillImage;
        [SerializeField] private Color  _healthyColor  = Color.green;
        [SerializeField] private Color  _criticalColor = Color.red;

        private PlayerHealth _health;

        private void Start()
        {
            _health = PlayerController.Instance?.GetComponent<PlayerHealth>();
            if (_health == null)
            {
                _health = FindFirstObjectByType<PlayerHealth>();
            }

            if (_health != null)
            {
                _health.OnHealthChanged += UpdateBar;
                UpdateBar(_health.CurrentHP, _health.MaxHP);
            }
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.OnHealthChanged -= UpdateBar;
        }

        private void UpdateBar(float current, float max)
        {
            if (_slider == null) return;
            float t = current / max;
            _slider.value = t;

            if (_fillImage != null)
                _fillImage.color = Color.Lerp(_criticalColor, _healthyColor, t);
        }
    }
}
