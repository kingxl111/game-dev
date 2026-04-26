using TMPro;
using UnityEngine;
using ReactorBreach.Player;

namespace ReactorBreach.UI
{
    public class ToolHelpPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _body;

        private ToolManager _toolManager;

        private void Start()
        {
            _toolManager = PlayerController.Instance?.GetComponent<ToolManager>();
            if (_toolManager == null)
                _toolManager = FindFirstObjectByType<ToolManager>();

            if (_toolManager != null)
            {
                _toolManager.OnToolSwitched += OnToolSwitched;
                OnToolSwitched(_toolManager.CurrentIndex);
            }
            else
                SetText("Инструменты", "1/2/3 - выбрать инструмент. ЛКМ/ПКМ - действия.");
        }

        private void OnDestroy()
        {
            if (_toolManager != null)
                _toolManager.OnToolSwitched -= OnToolSwitched;
        }

        private void OnToolSwitched(int index)
        {
            switch (index)
            {
                case 0:
                    SetText("Сварка [1]", "ЛКМ по двум объектам: соединить балки, анкеры или другие свариваемые детали.");
                    break;
                case 1:
                    SetText("Гравитация [2]", "ЛКМ: утяжелить объект для плиты. ПКМ: облегчить ящик, чтобы сдвинуть его телом.");
                    break;
                case 2:
                    SetText("Пена [3]", "ЛКМ: залить поверхность или врага. Пена замедляет, а при долгом удержании может нейтрализовать.");
                    break;
                default:
                    SetText("Инструменты", "1/2/3 - выбрать инструмент. ЛКМ/ПКМ - действия.");
                    break;
            }
        }

        private void SetText(string title, string body)
        {
            if (_title != null) _title.text = title;
            if (_body != null) _body.text = body;
        }
    }
}
