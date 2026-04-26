using UnityEngine;
using TMPro;

namespace ReactorBreach.UI
{
    /// <summary>
    /// Центральный контроллер HUD. Управляет показом предупреждений.
    /// Остальные виджеты (HealthBar, ToolIndicator и т.д.) работают независимо.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI _warningText;
        [SerializeField] private float _warningDuration = 2.5f;
        [SerializeField] private float _mechanicNoteDuration = 2.8f;
        [SerializeField] private Color   _mechanicNoteColor  = new Color(0.45f, 0.95f, 0.55f, 1f);
        [SerializeField] private Color   _warningDefaultColor = new Color(1f, 0.92f, 0.2f, 1f);

        private float _warningTimer;
        private bool  _isMechanicNote;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Update()
        {
            if (_warningTimer > 0f)
            {
                _warningTimer -= Time.deltaTime;
                if (_warningTimer <= 0f && _warningText != null)
                {
                    _warningText.text = string.Empty;
                    _isMechanicNote   = false;
                    _warningText.color = _warningDefaultColor;
                }
            }
        }

        public void ShowWarning(string message)
        {
            if (_warningText == null) return;
            _isMechanicNote  = false;
            _warningText.color = _warningDefaultColor;
            _warningText.text = message;
            _warningTimer     = _warningDuration;
        }

        /// <summary>Короткое подтверждение, что сработала механика (сварка, гравитация, плита).</summary>
        public void ShowMechanicNote(string message, float? duration = null)
        {
            if (_warningText == null) return;
            _isMechanicNote = true;
            _warningText.color = _mechanicNoteColor;
            _warningText.text  = message;
            _warningTimer      = duration ?? _mechanicNoteDuration;
        }

        /// <summary>Статический прокси для вызова из non-MonoBehaviour классов.</summary>
        public static void ShowWarningStatic(string message)
        {
            Instance?.ShowWarning(message);
        }

        public static void ShowMechanicNoteStatic(string message, float? duration = null)
        {
            Instance?.ShowMechanicNote(message, duration);
        }
    }
}
