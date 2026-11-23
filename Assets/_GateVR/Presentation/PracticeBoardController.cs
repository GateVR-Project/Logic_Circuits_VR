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
        [SerializeField] private bool developmentFreeNavigation;

        private PracticeSessionService _session;
        private LessonProgress _progress;
        private LessonTaskPanel _tasks;

        private void Awake()
        {
            _session = new PracticeSessionService();
            _progress = new LessonProgress(_session.CurrentExercise);
            EnsureGateButtons();
            RefreshNextButton();
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

            if (switchA != null)
                switchA.Interacted += OnSwitchA;

            if (switchB != null)
                switchB.Interacted += OnSwitchB;

            if (connector != null)
                connector.WiringChanged += OnWiringChanged;
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

            if (switchA != null)
                switchA.Interacted -= OnSwitchA;

            if (switchB != null)
                switchB.Interacted -= OnSwitchB;

            if (connector != null)
                connector.WiringChanged -= OnWiringChanged;
        }

        private void Start()
        {
            ShowCurrentExercise();
            HighlightSelectedGate();
            RefreshWiring();
            RefreshProgressUi();
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
            RefreshWiring();
            _progress.NoteValidation(result.IsSuccess);
            RefreshProgressUi();

            string message = result.FeedbackMessage;
            if (result.IsSuccess && _progress.IsPracticeComplete)
                message += " Puedes avanzar con Siguiente.";
            else if (result.IsSuccess)
                message += " Completa las tareas pendientes para avanzar.";

            SetFeedback(message, result.IsSuccess ? successColor : errorColor);
            RefreshViews();
        }

        public void SelectNextExercise()
        {
            if (!developmentFreeNavigation && !_progress.IsPracticeComplete)
            {
                SetFeedback("Completa las tareas de esta práctica para avanzar.", hintColor);
                return;
            }

            PracticeExercise exercise = _session.AdvanceExercise();
            _progress.Load(exercise);
            LoadPracticeStation(exercise);
            RefreshProgressUi();
        }

        public void SelectAnd()
        {
            ApplyGate(GateType.AND, true);
        }

        public void SelectOr()
        {
            ApplyGate(GateType.OR, true);
        }

        public void SelectNot()
        {
            ApplyGate(GateType.NOT, true);
        }

        public void ResetPractice()
        {
            if (!ClearCircuit())
                return;

            _progress.NoteCircuitReset();
            ShowPracticeTexts();
            RefreshProgressUi();
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

            ApplyGate(station, false);
            if (exercise.KeepCompatibleWiring)
                return;

            if (ClearCircuit())
                ShowCurrentExercise();
        }

        /// <summary>
        /// Cambia solo el nodo de la estación. La práctica activa no se modifica.
        /// AND y OR conservan A y B. NOT conserva A y libera B.
        /// </summary>
        private void ApplyGate(GateType type, bool studentChoice)
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
            if (studentChoice && activeGate.Node != null)
                _progress.NoteManualGate(activeGate.Node.GateName);

            RefreshWiring();
            ShowCurrentExercise();
            RefreshProgressUi();
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

        private void OnSwitchA()
        {
            NoteStudentSwitch("A");
        }

        private void OnSwitchB()
        {
            NoteStudentSwitch("B");
        }

        private void NoteStudentSwitch(string port)
        {
            _progress.NoteManualSwitch(port);
            RefreshProgressUi();
        }

        private void OnWiringChanged()
        {
            RefreshWiring();
            RefreshProgressUi();
        }

        private void RefreshWiring()
        {
            SwitchNode nodeA;
            SwitchNode nodeB;
            GateNode gate;
            LedNode bulb;
            if (!TryGetNodes(out nodeA, out nodeB, out gate, out bulb))
                return;

            var structure = _session.InspectStructure(nodeA, nodeB, gate, bulb);
            _progress.NoteWiring(structure.IsValid);
        }

        private void RefreshProgressUi()
        {
            if (_tasks != null)
                _tasks.Bind(_progress.Tasks, _progress.CompletedCount, _progress.TotalCount);

            RefreshNextButton();
        }

        private void RefreshNextButton()
        {
            if (nextButton == null)
                return;

            nextButton.interactable = developmentFreeNavigation || _progress.IsPracticeComplete;
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

            if (_tasks != null)
            {
                _tasks.SetModule(exercise.ModuleName);
                _tasks.SetTruthTable(FormatTruthTable(exercise));
            }

            if (exercise.UsesSingleInput)
            {
                SetFeedback("Conecta A con la compuerta y la salida con el LED. Luego prueba A y pulsa Comprobar.", hintColor);
                return;
            }

            SetFeedback("Conecta A, B y la salida Y. Prueba ambos interruptores y pulsa Comprobar.", hintColor);
        }

        private static string FormatTruthTable(PracticeExercise exercise)
        {
            if (exercise.UsesSingleInput)
            {
                return "<mspace=0.7em>A  Y\n"
                    + "0  " + Bit(exercise.ExpectedTruthTable(false, false)) + "\n"
                    + "1  " + Bit(exercise.ExpectedTruthTable(true, false)) + "</mspace>";
            }

            return "<mspace=0.7em>A B  Y\n"
                + "0 0  " + Bit(exercise.ExpectedTruthTable(false, false)) + "\n"
                + "0 1  " + Bit(exercise.ExpectedTruthTable(false, true)) + "\n"
                + "1 0  " + Bit(exercise.ExpectedTruthTable(true, false)) + "\n"
                + "1 1  " + Bit(exercise.ExpectedTruthTable(true, true)) + "</mspace>";
        }

        private static string Bit(bool value)
        {
            return value ? "1" : "0";
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
            Canvas canvasComponent = titleText != null
                ? titleText.GetComponentInParent<Canvas>()
                : FindAnyObjectByType<Canvas>();
            if (canvasComponent == null)
                return;

            RectTransform canvas = canvasComponent.transform as RectTransform;
            TMP_FontAsset font = titleText != null ? titleText.font : TMP_Settings.defaultFontAsset;
            _tasks = LessonTaskPanel.Mount(
                canvas,
                font,
                titleText,
                objectiveText,
                conditionText,
                feedbackText,
                checkButton,
                nextButton,
                resetButton,
                ref andButton,
                ref orButton,
                ref notButton);
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
