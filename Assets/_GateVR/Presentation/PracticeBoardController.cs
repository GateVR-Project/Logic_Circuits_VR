using System;
using System.Collections.Generic;
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
        private readonly List<GateView> _modules = new List<GateView>();
        private GateView _focused;
        private Vector3 _gateHome;
        private bool _gateHomeReady;

        private void Awake()
        {
            _session = new PracticeSessionService();
            _progress = new LessonProgress(_session.CurrentExercise);
            if (activeGate != null)
            {
                _gateHome = activeGate.transform.localPosition;
                _gateHomeReady = true;
                _modules.Add(activeGate);
                _focused = activeGate;
            }

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

            ListenGate(GateType.NAND, SelectNand);
            ListenGate(GateType.NOR, SelectNor);
            ListenGate(GateType.XOR, SelectXor);
            ListenGate(GateType.XNOR, SelectXnor);

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

            SilenceGate(GateType.NAND, SelectNand);
            SilenceGate(GateType.NOR, SelectNor);
            SilenceGate(GateType.XOR, SelectXor);
            SilenceGate(GateType.XNOR, SelectXnor);

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
            ConfigureStation(_session.CurrentExercise);
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

            IReadOnlyList<StationGate> station = CaptureStation();
            if (station.Count == 0)
            {
                SetFeedback("La estación no tiene compuertas activas.", errorColor);
                return;
            }

            var result = _session.ValidateCurrentCircuit(nodeA, nodeB, station, bulb);
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
            ConfigureStation(exercise);
            RefreshProgressUi();
        }

        public void SelectNand()
        {
            ApplyGate(GateType.NAND, true);
        }

        public void SelectNor()
        {
            ApplyGate(GateType.NOR, true);
        }

        public void SelectXor()
        {
            ApplyGate(GateType.XOR, true);
        }

        public void SelectXnor()
        {
            ApplyGate(GateType.XNOR, true);
        }

        public void FocusModule(int index)
        {
            if (index < 0 || index >= _modules.Count || !_modules[index].gameObject.activeInHierarchy)
                return;

            _focused = _modules[index];
            HighlightSelectedGate();
            if (_tasks != null)
                _tasks.SetStationModules(ActiveModuleCount(), index, FocusModule);
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
        /// Prepara los módulos de la práctica y su compuerta inicial. No llama a Comprobar.
        /// </summary>
        private void ConfigureStation(PracticeExercise exercise)
        {
            int needed = exercise.Structure == null ? 1 : exercise.Structure.Modules.Count;
            EnsureModules(needed);
            _focused = _modules.Count > 0 ? _modules[0] : activeGate;

            if (exercise.Structure == null)
            {
                GateType station;
                if (!GateView.TryParse(exercise.InitialGateType, out station))
                {
                    ShowCurrentExercise();
                    SetFeedback(
                        "La práctica pide una estación " + exercise.InitialGateType
                        + " y esa compuerta aún no está en el laboratorio.",
                        errorColor);
                    return;
                }

                ApplyGate(station, false);
            }
            else
            {
                for (int i = 0; i < exercise.Structure.Modules.Count && i < _modules.Count; i++)
                {
                    GateType station;
                    if (!GateView.TryParse(exercise.Structure.Modules[i].GateType, out station))
                        continue;

                    _focused = _modules[i];
                    ApplyGate(station, false);
                }

                _focused = _modules[0];
            }

            if (!exercise.KeepCompatibleWiring && ClearCircuit())
                ShowCurrentExercise();

            HighlightSelectedGate();
            if (_tasks != null)
                _tasks.SetStationModules(ActiveModuleCount(), 0, FocusModule);

            RefreshWiring();
        }

        /// <summary>
        /// Cambia solo el nodo de la estación. La práctica activa no se modifica.
        /// AND y OR conservan A y B. NOT conserva A y libera B.
        /// </summary>
        private void ApplyGate(GateType type, bool studentChoice)
        {
            GateView target = _focused != null ? _focused : activeGate;
            if (target == null || target.Node == null)
            {
                SetFeedback("Asigna la compuerta activa en el Practice Board.", errorColor);
                return;
            }

            GateNode previous = target.Node;
            bool ledFollowsGate = led != null && led.Node != null && ReferenceEquals(led.Node.Input, previous);
            bool changed = target.TryChangeType(type);

            if (changed && ledFollowsGate)
            {
                var moved = led.Node.ReplaceInput(target.Node);
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
                connector.RebindOutputWires(target);
                if (type == GateType.NOT)
                    connector.ReleaseGatePort(target, portB: true);
            }

            HighlightSelectedGate();
            RefreshViews();
            if (studentChoice && target.Node != null)
                _progress.NoteManualGate(target.Node.GateName);

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

            var nodes = new List<GateNode>();
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] != null && _modules[i].Node != null)
                    nodes.Add(_modules[i].Node);
            }

            if (nodes.Count == 0)
                nodes.Add(gate);

            _session.ResetCircuit(nodeA, nodeB, nodes, bulb);
            if (connector != null)
                connector.ClearAllWires();

            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] != null)
                    _modules[i].RefreshAppearance();
            }

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

            IReadOnlyList<StationGate> station = CaptureStation();
            if (station.Count == 0)
                return;

            var structure = _session.InspectStructure(nodeA, nodeB, station, bulb);
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
            if (_tasks == null)
                return;

            andButton = _tasks.GateButton(GateType.AND);
            orButton = _tasks.GateButton(GateType.OR);
            notButton = _tasks.GateButton(GateType.NOT);
        }

        private void ListenGate(GateType type, UnityEngine.Events.UnityAction action)
        {
            Button button = _tasks != null ? _tasks.GateButton(type) : null;
            if (button != null)
                button.onClick.AddListener(action);
        }

        private void SilenceGate(GateType type, UnityEngine.Events.UnityAction action)
        {
            Button button = _tasks != null ? _tasks.GateButton(type) : null;
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        private void EnsureModules(int count)
        {
            if (count < 1)
                count = 1;

            if (_modules.Count == 0 && activeGate != null)
            {
                _modules.Add(activeGate);
                _focused = activeGate;
            }

            while (_modules.Count < count && activeGate != null)
            {
                GateView clone = Instantiate(activeGate, activeGate.transform.parent);
                clone.name = "GateModule_" + (_modules.Count + 1);
                ClaimPins(clone);
                VrPointerBinding.BindGate(clone);
                _modules.Add(clone);
            }

            for (int i = 0; i < _modules.Count; i++)
            {
                GateView module = _modules[i];
                if (module == null)
                    continue;

                bool visible = i < count;
                if (!visible)
                {
                    if (module.gameObject.activeSelf && connector != null)
                        connector.ReleaseModule(module);

                    module.gameObject.SetActive(false);
                    continue;
                }

                module.gameObject.SetActive(true);
                PlaceModule(module, i, count);
            }
        }

        private void PlaceModule(GateView module, int index, int count)
        {
            Transform body = module.transform;
            Vector3 home = _gateHomeReady ? _gateHome : body.localPosition;
            if (count <= 1)
            {
                body.localPosition = home;
                body.localScale = new Vector3(0.25f, 0.25f, 0.25f);
                return;
            }

            float scale = count >= 4 ? 0.16f : count == 3 ? 0.18f : 0.2f;
            float step = count >= 4 ? 0.2f : count == 3 ? 0.24f : 0.32f;
            float start = -step * (count - 1) * 0.5f;
            float z = index % 2 == 0 ? 0.04f : -0.04f;
            body.localScale = new Vector3(scale, scale, scale);
            body.localPosition = new Vector3(home.x + start + step * index, home.y, home.z + z);
        }

        private static void ClaimPins(GateView gate)
        {
            InputPinView[] inputs = gate.GetComponentsInChildren<InputPinView>(true);
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null && !inputs[i].IsLedInput)
                    inputs[i].AssignGate(gate);
            }

            OutputPinView[] outputs = gate.GetComponentsInChildren<OutputPinView>(true);
            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] != null)
                    outputs[i].AssignGate(gate);
            }
        }

        private int ActiveModuleCount()
        {
            int count = 0;
            for (int i = 0; i < _modules.Count; i++)
            {
                if (_modules[i] != null && _modules[i].gameObject.activeInHierarchy)
                    count++;
            }

            return count;
        }

        private IReadOnlyList<StationGate> CaptureStation()
        {
            var station = new List<StationGate>();
            int visible = 0;
            for (int i = 0; i < _modules.Count; i++)
            {
                GateView module = _modules[i];
                if (module == null || !module.gameObject.activeInHierarchy || module.Node == null)
                    continue;

                visible++;
                station.Add(new StationGate("m" + visible, module.Node));
            }

            return station;
        }

        private void HighlightSelectedGate()
        {
            GateView target = _focused != null ? _focused : activeGate;
            GateType type = target != null ? target.Type : GateType.AND;
            Paint(andButton, type == GateType.AND);
            Paint(orButton, type == GateType.OR);
            Paint(notButton, type == GateType.NOT);
            Paint(ButtonOf(GateType.NAND), type == GateType.NAND);
            Paint(ButtonOf(GateType.NOR), type == GateType.NOR);
            Paint(ButtonOf(GateType.XOR), type == GateType.XOR);
            Paint(ButtonOf(GateType.XNOR), type == GateType.XNOR);
        }

        private Button ButtonOf(GateType type)
        {
            if (_tasks == null)
                return null;

            return _tasks.GateButton(type);
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
