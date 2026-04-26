using UnityEngine;
using UnityEngine.UI;

namespace ReactorBreach.UI
{
    /// <summary>
    /// Меняет внешний вид прицела в зависимости от активного инструмента и цели рейкаста.
    /// </summary>
    public class CrosshairController : MonoBehaviour
    {
        [SerializeField] private Image _crosshairImage;
        [SerializeField] private Sprite _defaultSprite;
        [SerializeField] private Sprite _weldReadySprite;
        [SerializeField] private Sprite _weldLockedSprite;
        [SerializeField] private Sprite _gravityReadySprite;
        [SerializeField] private Sprite _gravityBlockedSprite;
        [SerializeField] private Sprite _foamReadySprite;
        [SerializeField] private Sprite _foamEmptySprite;

        [SerializeField] private Color _defaultColor   = Color.white;
        [SerializeField] private Color _highlightColor = new Color(1f, 0.6f, 0f);
        [SerializeField] private Color _blockedColor   = Color.red;
        [SerializeField] private Color _foamColor      = new Color(0.5f, 0.9f, 0.5f);

        private Player.ToolManager _toolManager;

        private void Start()
        {
            _toolManager = Player.PlayerController.Instance?.GetComponent<Player.ToolManager>();
            if (_toolManager == null)
                _toolManager = FindFirstObjectByType<Player.ToolManager>();
        }

        private void Update()
        {
            if (_crosshairImage == null || _toolManager == null) return;

            var tool = _toolManager.CurrentTool;
            switch (_toolManager.CurrentIndex)
            {
                case 0: UpdateWeldCrosshair(tool as Tools.WeldTool); break;
                case 1: UpdateGravityCrosshair(tool as Tools.GravityTool); break;
                case 2: UpdateFoamCrosshair(tool as Tools.FoamTool); break;
                default:
                    _crosshairImage.sprite = _defaultSprite;
                    _crosshairImage.color  = _defaultColor;
                    break;
            }
        }

        private void UpdateWeldCrosshair(Tools.WeldTool weld)
        {
            if (weld == null) return;

            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            bool hit = Physics.Raycast(ray, out var info, weld.WeldRange);
            bool isWeldable = hit && info.collider.TryGetComponent<Environment.IWeldable>(out _);

            _crosshairImage.sprite = isWeldable ? _weldReadySprite   : _defaultSprite;
            _crosshairImage.color  = isWeldable ? _highlightColor : _defaultColor;
        }

        private void UpdateGravityCrosshair(Tools.GravityTool gravity)
        {
            if (gravity == null) return;

            if (!gravity.CanUse)
            {
                _crosshairImage.sprite = _gravityBlockedSprite;
                _crosshairImage.color  = _blockedColor;
                return;
            }

            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            bool hit = Physics.Raycast(ray, out var info, gravity.GravityRange);
            bool valid = hit && info.collider.TryGetComponent<Environment.IGravityAffectable>(out _);

            _crosshairImage.sprite = valid ? _gravityReadySprite : _defaultSprite;
            _crosshairImage.color  = valid ? new Color(0.7f, 0.3f, 1f) : _blockedColor;
        }

        private void UpdateFoamCrosshair(Tools.FoamTool foam)
        {
            if (foam == null) return;

            if (!foam.CanUse)
            {
                _crosshairImage.sprite = _foamEmptySprite;
                _crosshairImage.color  = Color.gray;
                return;
            }

            _crosshairImage.sprite = _foamReadySprite;
            _crosshairImage.color  = _foamColor;
        }
    }
}
