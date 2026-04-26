using UnityEngine;
using ReactorBreach.Environment;

namespace ReactorBreach.Player
{
    /// <summary>
    /// Отвечает за рейкаст от центра экрана и подсветку IWeldable объектов.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private float _interactRange = 3f;
        [SerializeField] private LayerMask _interactMask;

        private IWeldable _lastHighlighted;
        private Renderer _lastRenderer;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Update()
        {
            DoHighlight();
        }

        private void DoHighlight()
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            IWeldable hit = null;

            if (Physics.Raycast(ray, out RaycastHit info, _interactRange, _interactMask))
            {
                info.collider.TryGetComponent(out hit);
            }

            if (hit == _lastHighlighted) return;

            // Remove highlight from previous
            if (_lastRenderer != null)
            {
                _lastRenderer.material.DisableKeyword("_EMISSION");
                _lastHighlighted = null;
                _lastRenderer    = null;
            }

            // Add highlight to new
            if (hit != null && hit.CanBeWelded)
            {
                _lastHighlighted = hit;
                _lastRenderer    = hit.WeldTransform.GetComponent<Renderer>();
                if (_lastRenderer != null)
                {
                    _lastRenderer.material.EnableKeyword("_EMISSION");
                    _lastRenderer.material.SetColor(EmissionColor, Color.yellow * 2f);
                }
            }
        }

        public bool TryGetInteractable(float range, out RaycastHit hit, LayerMask mask)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            return Physics.Raycast(ray, out hit, range, mask);
        }
    }
}
