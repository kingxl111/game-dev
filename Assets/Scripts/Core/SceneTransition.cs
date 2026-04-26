using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReactorBreach.Core
{
    public class SceneTransition : MonoBehaviour
    {
        [SerializeField] private string _targetScene;
        [SerializeField] private float _fadeDuration = 1f;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            StartCoroutine(TransitionRoutine());
        }

        private IEnumerator TransitionRoutine()
        {
            yield return UIFade.FadeOut(_fadeDuration);
            SceneManager.LoadScene(_targetScene);
            yield return UIFade.FadeIn(_fadeDuration);
        }
    }

    /// <summary>
    /// Статический хелпер для fade-эффектов через CanvasGroup.
    /// CanvasGroup должна быть на объекте с тегом "UIFade" в сцене.
    /// </summary>
    public static class UIFade
    {
        private static CanvasGroup _fadeGroup;

        private static CanvasGroup GetFadeGroup()
        {
            if (_fadeGroup != null) return _fadeGroup;
            var obj = GameObject.FindWithTag("UIFade");
            if (obj != null)
                _fadeGroup = obj.GetComponent<CanvasGroup>();
            return _fadeGroup;
        }

        public static IEnumerator FadeOut(float duration)
        {
            var group = GetFadeGroup();
            if (group == null) yield break;

            float elapsed = 0f;
            group.alpha = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = elapsed / duration;
                yield return null;
            }
            group.alpha = 1f;
        }

        public static IEnumerator FadeIn(float duration)
        {
            var group = GetFadeGroup();
            if (group == null) yield break;

            float elapsed = 0f;
            group.alpha = 1f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - elapsed / duration;
                yield return null;
            }
            group.alpha = 0f;
        }
    }
}
