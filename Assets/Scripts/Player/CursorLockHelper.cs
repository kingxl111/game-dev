using UnityEngine;

namespace ReactorBreach.Player
{
    /// <summary>
    /// Прячет курсор и блокирует его в центре экрана при старте сцены.
    /// Снимает блокировку, когда GameManager переходит в Paused/MainMenu/GameOver.
    /// </summary>
    public class CursorLockHelper : MonoBehaviour
    {
        private void OnEnable()
        {
            Lock();
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.OnGameStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (Core.GameManager.Instance != null)
                Core.GameManager.Instance.OnGameStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(Core.GameState state)
        {
            if (state == Core.GameState.Playing) Lock();
            else Unlock();
        }

        private static void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }

        private static void Unlock()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
    }
}
