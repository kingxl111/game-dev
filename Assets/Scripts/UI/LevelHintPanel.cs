using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ReactorBreach.UI
{
    [DefaultExecutionOrder(-1000)]
    public class LevelHintPanel : MonoBehaviour
    {
        public static LevelHintPanel Instance { get; private set; }

        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _body;
        [SerializeField] private Button _dismissButton;

        private bool _open;
        private string _dismissContext;

        private static readonly HashSet<string> DismissedHintContexts = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DismissedHintContexts.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (_panel != null) _panel.SetActive(false);
            if (_dismissButton != null) _dismissButton.onClick.AddListener(Hide);
        }

        private void Update()
        {
            if (!_open) return;

            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame
                            || kb.enterKey.wasPressedThisFrame
                            || kb.spaceKey.wasPressedThisFrame))
                Hide();
        }

        public static event Action<string> OnHintDismissed;

        public void Show(string title, string body) => Show(title, body, null);

        public void Show(string title, string body, string dismissContext)
        {
            if (_title != null) _title.text = title;
            if (_body  != null) _body.text  = body;
            _dismissContext = dismissContext;
            if (_panel != null) _panel.SetActive(true);
            _open = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            _open = false;
            var ctx = _dismissContext;
            _dismissContext = null;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
            if (!string.IsNullOrEmpty(ctx))
                DismissedHintContexts.Add(ctx);
            OnHintDismissed?.Invoke(ctx);
        }

        public static bool WasHintContextDismissed(string context)
        {
            return !string.IsNullOrEmpty(context) && DismissedHintContexts.Contains(context);
        }
    }
}
