using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using ReactorBreach.Data;

namespace ReactorBreach.Core
{
    public enum GameState { Menu, Playing, Paused, GameOver, LevelComplete }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }
        public int CurrentLevelIndex { get; private set; }

        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void SetState(GameState state)
        {
            CurrentState = state;
            OnGameStateChanged?.Invoke(state);
        }

        public void StartGame()
        {
            CurrentLevelIndex = 1;
            LoadLevel(CurrentLevelIndex);
        }

        public void PauseGame()
        {
            if (CurrentState != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (CurrentState != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void CompleteLevel()
        {
            SetState(GameState.LevelComplete);
        }

        public void GameOver()
        {
            SetState(GameState.GameOver);
        }

        public void RestartCurrentLevelFromGameOver()
        {
            Time.timeScale = 1f;
            SetState(GameState.Playing);
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex, LoadSceneMode.Single);
            else                       SceneManager.LoadScene(scene.name);
        }

        public void LoadLevel(int index)
        {
            CurrentLevelIndex = index;
            Time.timeScale = 1f;
            SceneManager.LoadScene(index);
            SetState(GameState.Playing);
        }

        public void LoadNextLevel()
        {
            LoadLevel(CurrentLevelIndex + 1);
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameConstants.SceneMainMenu);
            SetState(GameState.Menu);
        }

        public void MarkLevelSceneAsPlayingIfNeeded()
        {
            if (CurrentState == GameState.Menu)
                SetState(GameState.Playing);
        }
    }
}
