using System;
using GateVR.Application;
using GateVR.Domain.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GateVR.Presentation
{
    /// <summary>
    /// La práctica activa fija los requisitos. La compuerta elegida es la decisión del estudiante.
    /// Comprobar es quien compara ambas cosas.
    /// </summary>
    public class PracticeBoardController : MonoBehaviour
    {
        [SerializeField] private SwitchView switchA;
        [SerializeField] private SwitchView switchB;
        [SerializeField] private GateView activeGate;
        [SerializeField] private LedView led;
        [SerializeField] private PinConnectorManager connector;

        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private TextMeshProUGUI conditionText;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [SerializeField] private Button checkButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button andButton;
        [SerializeField] private Button orButton;
        [SerializeField] private Button notButton;

        [SerializeField] private Color hintColor = Color.white;
        [SerializeField] private Color successColor = new Color(0.5f, 0.95f, 0.55f);
        [SerializeField] private Color errorColor = new Color(1f, 0.45f, 0.4f);

        private static Sprite _buttonSprite;
        private PracticeSessionService _session;

        private void Awake()
        {
            _session = new PracticeSessionService();
            EnsureGateButtons();
        }

        private void OnEnable()
        {
            if (checkButton != null)
                checkButton.onClick.AddListener(CheckCircuit);

            if (nextButton != null)
                nextButton.onClick.AddListener(SelectNextExercise);

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetPractice);

            if (andButton != null)
                andButton.onClick.AddListener(SelectAnd);

            if (orButton != null)
                orButton.onClick.AddListener(SelectOr);

            if (notButton != null)
                notButton.onClick.AddListener(SelectNot);
        }

        private void OnDisable()
        {
            if (checkButton != null)
                checkButton.onClick.RemoveListener(CheckCircuit);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(SelectNextExercise);

            if (resetButton != null)
                resetButton.onClick.RemoveListener(ResetPractice);

            if (andButton != null)
                andButton.onClick.RemoveListener(SelectAnd);

            if (orButton != null)
                orButton.onClick.RemoveListener(SelectOr);

            if (notButton != null)
                notButton.onClick.RemoveListener(SelectNot);
        }

        private void Start()
        {
            ShowCurrentExercise();
            HighlightSelectedGate();
        }

        public void CheckCircuit()
        {
            SwitchNode nodeA;
            SwitchNode nodeB;
            GateNode gate;
            LedNode bulb;
            if (!TryGetNodes(out nodeA, out nodeB, out gate, out bulb))
                return;

            var result = _session.ValidateCurrentCircuit(nodeA, nodeB, gate, bulb);
            SetFeedback(result.FeedbackMessage, result.IsSuccess ? successColor : errorColor);
            RefreshViews();
        }

        public void SelectNextExercise()
        {
            PracticeExercise exercise = _session.AdvanceExercise();
            LoadPracticeStation(exercise);
        }

        public void SelectAnd()
        {
            ApplyGate(GateType.AND);
        }

        public void SelectOr()
        {
            ApplyGate(GateType.OR);
        }

        public void SelectNot()
        {
            ApplyGate(GateType.NOT);
        }

        public void ResetPractice()
        {
            if (!ClearCircuit())
                return;

            ShowPracticeTexts();
            SetFeedback(
                "Circuito reiniciado. La práctica activa sigue siendo " + _session.CurrentExercise.Title + ".",
                hintColor);
        }

        /// <summary>
        /// Carga la estación de la práctica. No llama a Comprobar.
        /// </summary>
        private void LoadPracticeStation(PracticeExercise exercise)
        {
            GateType station;
            if (!TryStationType(exercise.InitialGateType, out station))
            {
                ShowCurrentExercise();
                SetFeedback(
                    "La práctica pide una estación " + exercise.InitialGateType
                    + " y esa compuerta aún no está en el laboratorio.",
                    errorColor);
                return;
            }

            ApplyGate(station);
            if (exercise.KeepCompatibleWiring)
                return;

            if (ClearCircuit())
                ShowCurrentExercise();
        }

        /// <summary>
        /// Cambia solo el nodo de la estación. La práctica activa no se modifica.
        /// AND y OR conservan A y B. NOT conserva A y libera B.
        /// </summary>
        private void ApplyGate(GateType type)
        {
            if (activeGate == null || activeGate.Node == null)
            {
                SetFeedback("Asigna la compuerta activa en el Practice Board.", errorColor);
                return;
            }

            GateNode previous = activeGate.Node;
            bool ledFollowsGate = led != null && led.Node != null && ReferenceEquals(led.Node.Input, previous);
            bool changed = activeGate.TryChangeType(type);

            if (changed && ledFollowsGate)
            {
                var moved = led.Node.ReplaceInput(activeGate.Node);
                if (!moved.IsValid)
                {
                    SetFeedback(moved.ErrorMessage, errorColor);
                    HighlightSelectedGate();
                    RefreshViews();
                    return;
                }
            }

            if (changed && connector != null)
            {
                connector.RebindOutputWires(activeGate);
                if (type == GateType.NOT)
                    connector.ReleaseGatePort(activeGate, portB: true);
            }

            HighlightSelectedGate();
            RefreshViews();
            ShowCurrentExercise();
        }

        private bool ClearCircuit()
        {
            SwitchNode nodeA;
            SwitchNode nodeB;
            GateNode gate;
            LedNode bulb;
            if (!TryGetNodes(out nodeA, out nodeB, out gate, out bulb))
                return false;

            _session.ResetCircuit(nodeA, nodeB, gate, bulb);
            if (connector != null)
                connector.ClearAllWires();

            if (activeGate != null)
                activeGate.RefreshAppearance();

            RefreshViews();
            return true;
        }

        private static bool TryStationType(string gateName, out GateType type)
        {
            if (string.Equals(gateName, "AND", StringComparison.OrdinalIgnoreCase))
            {
                type = GateType.AND;
                return true;
            }

            if (string.Equals(gateName, "OR", StringComparison.OrdinalIgnoreCase))
            {
                type = GateType.OR;
                return true;
            }

            if (string.Equals(gateName, "NOT", StringComparison.OrdinalIgnoreCase))
            {
                type = GateType.NOT;
                return true;
            }

            type = GateType.AND;
            return false;
        }

        private void ShowCurrentExercise()
        {
            PracticeExercise exercise = _session.CurrentExercise;
            ShowPracticeTexts();

            if (exercise.UsesSingleInput)
            {
                SetFeedback(
                    "Práctica activa: " + exercise.RequiredGateType
                    + ". Conecta el interruptor A a la entrada A y la salida al LED.",
                    hintColor);
                return;
            }

            SetFeedback(
                "Práctica activa: " + exercise.RequiredGateType
                + ". Conecta los interruptores a la compuerta y la salida al LED.",
                hintColor);
        }

        private void ShowPracticeTexts()
        {
            PracticeExercise exercise = _session.CurrentExercise;
            if (titleText != null)
                titleText.text = exercise.Title;

            if (objectiveText != null)
                objectiveText.text = exercise.Objective;

            if (conditionText != null)
                conditionText.text = exercise.LogicConditionText;
        }

        private bool TryGetNodes(out SwitchNode nodeA, out SwitchNode nodeB, out GateNode gate, out LedNode bulb)
        {
            nodeA = switchA != null ? switchA.Node : null;
            nodeB = switchB != null ? switchB.Node : null;
            gate = activeGate != null ? activeGate.Node : null;
            bulb = led != null ? led.Node : null;

            if (nodeA != null && nodeB != null && gate != null && bulb != null)
                return true;

            SetFeedback("Asigna Switch A, Switch B, la compuerta activa y el LED en el Practice Board.", errorColor);
            return false;
        }

        private void RefreshViews()
        {
            if (switchA != null)
                switchA.RefreshVisual();

            if (switchB != null)
                switchB.RefreshVisual();

            if (led != null)
                led.RefreshVisual();
        }

        private void SetFeedback(string message, Color color)
        {
            if (feedbackText == null)
                return;

            feedbackText.text = message;
            feedbackText.color = color;
        }

        private void EnsureGateButtons()
        {
            Transform parent = titleText != null ? titleText.transform.parent : null;
            if (parent == null)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                    parent = canvas.transform;
            }

            if (parent == null)
                return;

            TMP_FontAsset font = titleText != null ? titleText.font : TMP_Settings.defaultFontAsset;
            EnsureCaption(parent, font);
            if (andButton == null)
                andButton = EnsureButton(parent, "Btn_AND", "AND", new Vector2(250f, 150f), font);
            if (orButton == null)
                orButton = EnsureButton(parent, "Btn_OR", "OR", new Vector2(250f, 100f), font);
            if (notButton == null)
                notButton = EnsureButton(parent, "Btn_NOT", "NOT", new Vector2(250f, 50f), font);
        }

        private static void EnsureCaption(Transform parent, TMP_FontAsset font)
        {
            Transform existing = parent.Find("Txt_GateSelector");
            TextMeshProUGUI text;
            if (existing != null)
            {
                text = existing.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                GameObject caption = new GameObject("Txt_GateSelector", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                caption.layer = parent.gameObject.layer;
                caption.transform.SetParent(parent, false);
                RectTransform rect = caption.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(180f, 28f);
                rect.anchoredPosition = new Vector2(250f, 195f);
                text = caption.GetComponent<TextMeshProUGUI>();
            }

            if (text == null)
                return;

            text.text = "Compuerta";
            text.fontSize = 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            if (font != null)
                text.font = font;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Vector2 position, TMP_FontAsset font)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Button found = existing.GetComponent<Button>();
                if (found != null)
                    return found;
            }

            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.layer = parent.gameObject.layer;
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(150f, 36f);
            rect.anchoredPosition = position;

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = ButtonSprite();
            image.type = Image.Type.Simple;
            image.color = new Color(0.16f, 0.24f, 0.36f, 0.95f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.layer = buttonObject.layer;
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            if (font != null)
                text.font = font;

            return button;
        }

        private static Sprite ButtonSprite()
        {
            if (_buttonSprite != null)
                return _buttonSprite;

            _buttonSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (_buttonSprite != null)
                return _buttonSprite;

            Texture2D texture = Texture2D.whiteTexture;
            _buttonSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _buttonSprite;
        }

        private void HighlightSelectedGate()
        {
            GateType type = activeGate != null ? activeGate.Type : GateType.AND;
            Paint(andButton, type == GateType.AND);
            Paint(orButton, type == GateType.OR);
            Paint(notButton, type == GateType.NOT);
        }

        private static void Paint(Button button, bool selected)
        {
            if (button == null)
                return;

            Image image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();
            if (image == null)
                return;

            image.color = selected
                ? new Color(0.95f, 0.72f, 0.2f, 1f)
                : new Color(0.16f, 0.24f, 0.36f, 0.95f);
        }
    }
}
