using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using ReactorBreach.Core;

namespace ReactorBreach.UI
{
    /// <summary>
    /// Оверлей Game Over: появляется когда GameManager переходит в состояние GameOver.
    /// Жмёшь R / Enter / Space или клик мыши — перезапуск текущей сцены.
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;

        private bool _showing;

        private void Start()
        {
            if (_panel != null) _panel.SetActive(false);
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
            if (state == GameState.GameOver) Show();
        }

        private void Show()
        {
            _showing = true;
            if (_panel != null) _panel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            Time.timeScale   = 0f;
        }

        private void Update()
        {
            if (!_showing) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            bool restart = (kb != null && (kb.rKey.wasPressedThisFrame
                                          || kb.enterKey.wasPressedThisFrame
                                          || kb.spaceKey.wasPressedThisFrame))
                        || (mouse != null && mouse.leftButton.wasPressedThisFrame);

            if (restart)
            {
                _showing = false;
                if (GameManager.Instance != null)
                    GameManager.Instance.RestartCurrentLevelFromGameOver();
                else
                {
                    Time.timeScale = 1f;
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }
        }
    }
}
