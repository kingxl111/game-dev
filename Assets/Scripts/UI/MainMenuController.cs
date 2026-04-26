using UnityEngine;
using UnityEngine.UI;
using ReactorBreach.Core;
using ReactorBreach.Systems;

namespace ReactorBreach.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private GameObject _settingsPanel;

        private void Start()
        {
            _newGameButton?.onClick.AddListener(OnNewGame);
            _continueButton?.onClick.AddListener(OnContinue);
            _settingsButton?.onClick.AddListener(OnSettings);
            _quitButton?.onClick.AddListener(OnQuit);

            var save = SaveSystem.Load();
            if (_continueButton != null)
                _continueButton.interactable = save.LastCompletedLevel > 0;

            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnNewGame()
        {
            SaveSystem.DeleteSave();
            GameManager.Instance?.StartGame();
        }

        private void OnContinue()
        {
            var save = SaveSystem.Load();
            int level = Mathf.Min(save.LastCompletedLevel + 1, 5);
            GameManager.Instance?.LoadLevel(level);
        }

        private void OnSettings()
        {
            if (_settingsPanel != null)
                _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
