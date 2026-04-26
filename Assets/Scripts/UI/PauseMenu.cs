using UnityEngine;
using ReactorBreach.Core;

namespace ReactorBreach.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject _pausePanel;

        private void Start()
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(state == GameState.Paused);
        }

        public void OnResumeClicked()   => GameManager.Instance?.ResumeGame();
        public void OnMenuClicked()     => GameManager.Instance?.ReturnToMenu();
        public void OnQuitClicked()     => Application.Quit();
    }
}
