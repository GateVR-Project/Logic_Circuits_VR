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
        [SerializeField] private float moduleSpacing = 0.08f;
        [SerializeField] private float pinClearance = 0.055f;
        [SerializeField] private float moduleRowSpacing = 0.26f;
        [SerializeField] private float boardMargin = 0.045f;
        [SerializeField] private float labelOffset = 0.9f;

        private PracticeSessionService _session;
        private LessonProgress _progress;
        private LessonTaskPanel _tasks;
        private readonly List<GateView> _modules = new List<GateView>();
        private GateView _focused;
        private Vector3 _gateHome;
        private Vector3 _switchAHome;
        private Vector3 _switchBHome;
        private Vector3 _ledHome;
        private bool _homesReady;
        private bool _configuring;
        private bool _metricsReady;
        private Transform _placa;
        private Vector3 _placaHomeScale = Vector3.one;
        private float _gateHomeScale = 0.25f;
        private StationLayout.Area _boardHome;
        private StationLayout.Area _tableArea;
        private StationLayout.Span _gateUnit = new StationLayout.Span { Left = 1.02f, Right = 1.03f, Near = 0.45f, Far = 0.45f };
        private StationLayout.Span _ledSpan = new StationLayout.Span { Left = 0.11f, Right = 0.07f, Near = 0.04f, Far = 0.04f };
        private StationLayout.Span _switchSpan = new StationLayout.Span { Left = 0.09f, Right = 0.22f, Near = 0.08f, Far = 0.08f };

        private void Awake()
        {
            _session = new PracticeSessionService();
            _progress = new LessonProgress(_session.CurrentExercise);
            if (activeGate != null)
            {
                _gateHome = activeGate.transform.localPosition;
                _modules.Add(activeGate);
                _focused = activeGate;
            }

            if (switchA != null)
                _switchAHome = switchA.transform.localPosition;
            if (switchB != null)
                _switchBHome = switchB.transform.localPosition;
            if (led != null)
                _ledHome = led.transform.localPosition;
            _homesReady = activeGate != null;

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
            ListenNav("Fundamentos", OpenFundamentos);
            ListenNav("Aplicación Real", OpenAplicacion);
            ListenNav("Diagnóstico", OpenDiagnostico);

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
            SilenceNav("Fundamentos", OpenFundamentos);
            SilenceNav("Aplicación Real", OpenAplicacion);
            SilenceNav("Diagnóstico", OpenDiagnostico);

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
            {
                PracticeExercise upcoming = _session.PeekNextExercise();
                message += upcoming != null && upcoming.ModuleName != _session.CurrentExercise.ModuleName
                    ? " Siguiente abre " + upcoming.ModuleName + "."
                    : " Puedes avanzar con Siguiente.";
            }
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
            RefreshModuleButtons();
            RefreshFocusVisuals();
        }

        public void OpenFundamentos()
        {
            OpenModule("Fundamentos");
        }

        public void OpenAplicacion()
        {
            OpenModule("Aplicación Real");
        }

        public void OpenDiagnostico()
        {
            OpenModule("Diagnóstico");
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
            ConfigureStation(_session.CurrentExercise, true);
            _progress.NoteCircuitReset();
            RefreshWiring();
            ShowPracticeTexts();
            RefreshProgressUi();
            SetFeedback(
                "Circuito reiniciado. La práctica activa sigue siendo " + _session.CurrentExercise.Title + ".",
                hintColor);
        }

        /// <summary>
        /// Prepara los módulos de la práctica y su compuerta inicial. No llama a Comprobar.
        /// </summary>
        private void ConfigureStation(PracticeExercise exercise, bool restoreInitial = false)
        {
            _configuring = true;
            try
            {
                int needed = exercise.Structure == null ? 1 : exercise.Structure.Modules.Count;
                EnsureModules(needed);
                _focused = _modules.Count > 0 ? _modules[0] : activeGate;
                ApplyInitialTypes(exercise);
                LayoutStation(exercise);

                bool rebuildWires = restoreInitial || exercise.Preset != null || !exercise.KeepCompatibleWiring;
                if (rebuildWires)
                    ClearCircuit();

                if (exercise.Preset != null)
                    ApplyPreset(exercise);

                _focused = _modules.Count > 0 ? _modules[0] : activeGate;
                RefreshFocusVisuals();
                RefreshModuleButtons();
            }
            finally
            {
                _configuring = false;
            }

            ShowCurrentExercise();
            RefreshWiring();
        }

        private void ApplyInitialTypes(PracticeExercise exercise)
        {
            int count = ActiveModuleCount();
            for (int i = 0; i < count && i < _modules.Count; i++)
            {
                string typeName = exercise.InitialGateType;
                if (exercise.InitialModuleTypes != null && i < exercise.InitialModuleTypes.Count)
                    typeName = exercise.InitialModuleTypes[i];
                else if (exercise.Structure != null && i < exercise.Structure.Modules.Count)
                    typeName = exercise.Structure.Modules[i].GateType;

                GateType station;
                if (!GateView.TryParse(typeName, out station))
                {
                    SetFeedback(
                        "La práctica pide una estación " + typeName
                        + " y esa compuerta aún no está en el laboratorio.",
                        errorColor);
                    continue;
                }

                _focused = _modules[i];
                ApplyGate(station, false);
            }
        }

        private void OpenModule(string moduleName)
        {
            for (int i = 0; i < _session.Exercises.Count; i++)
            {
                PracticeExercise exercise = _session.Exercises[i];
                if (!string.Equals(exercise.ModuleName, moduleName, StringComparison.Ordinal))
                    continue;

                _session.SelectExercise(exercise.Id);
                _progress.Load(exercise);
                ConfigureStation(exercise);
                RefreshProgressUi();
                SetFeedback("Submódulo " + moduleName + ". " + exercise.Title + ".", hintColor);
                return;
            }
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

            if (!_configuring)
            {
                RefreshWiring();
                ShowCurrentExercise();
                RefreshProgressUi();
                RefreshModuleButtons();
                RefreshFocusVisuals();
            }
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
            if (_configuring)
                return;

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
                int index = _session.IndexInModule(exercise);
                int total = _session.CountInModule(exercise.ModuleName);
                _tasks.SetModule(exercise.ModuleName + "  ·  Práctica " + index + " de " + total);
                _tasks.SetTruthTable(FormatTruthTable(exercise));
                HighlightNav(exercise.ModuleName);
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
            }
        }

        private void LayoutStation(PracticeExercise exercise)
        {
            int count = ActiveModuleCount();
            EnsureLayoutMetrics();
            int[] columns = new int[count];
            float[] lanes = new float[count];
            StationLayout.AssignColumns(exercise.Structure, count, columns, lanes);

            // Un paso fijo de 0.46 m, medido desde el interruptor, dejaba el LED
            // pasado el borde derecho de la placa (x > 0.775) y junto al panel.
            // Aquí la fila se centra en el rectángulo medido de Placa.
            float preferredGap = Mathf.Clamp(moduleSpacing, 0.05f, 0.12f);
            float minimumGap = Mathf.Clamp(pinClearance, 0.04f, preferredGap);
            float laneSpacing = Mathf.Clamp(moduleRowSpacing, 0.16f, 0.34f);
            float edge = Mathf.Clamp(boardMargin, 0.03f, 0.08f);
            StationLayout.Plan plan = StationLayout.Arrange(
                _boardHome,
                _tableArea,
                _gateUnit,
                _ledSpan,
                _switchSpan,
                _switchAHome.x,
                _switchAHome.z,
                _switchBHome.x,
                _switchBHome.z,
                _gateHome.x,
                _gateHome.z,
                _ledHome.x,
                _ledHome.z,
                _gateHomeScale,
                count,
                columns,
                lanes,
                0.2f,
                0.14f,
                preferredGap,
                minimumGap,
                laneSpacing,
                edge);

            ApplyBoardSize(plan.Board);
            if (switchA != null)
                switchA.transform.localPosition = new Vector3(plan.SwitchAX, _switchAHome.y, plan.SwitchAZ);
            if (switchB != null)
                switchB.transform.localPosition = new Vector3(plan.SwitchBX, _switchBHome.y, plan.SwitchBZ);
            if (led != null)
                led.transform.localPosition = new Vector3(plan.LedX, _ledHome.y, plan.LedZ);

            for (int i = 0; i < count && i < _modules.Count; i++)
            {
                GateView module = _modules[i];
                if (module == null)
                    continue;

                float x = i < plan.GateX.Length ? plan.GateX[i] : _gateHome.x;
                float z = i < plan.GateZ.Length ? plan.GateZ[i] : _gateHome.z;
                module.transform.localScale = new Vector3(plan.GateScale, plan.GateScale, plan.GateScale);
                module.transform.localPosition = new Vector3(x, _gateHome.y, z);
                module.SetLabelOffset(labelOffset);
            }
        }

        private void EnsureLayoutMetrics()
        {
            if (_metricsReady)
                return;

            Transform lab = transform.parent != null ? transform.parent : transform;
            _placa = FindNamed(lab, "Placa");
            if (_placa != null)
                _placaHomeScale = _placa.localScale;

            _boardHome = MeasureArea(lab, _placa, new StationLayout.Area
            {
                MinX = -0.775f,
                MaxX = 0.775f,
                MinZ = -0.475f,
                MaxZ = 0.475f
            });
            Transform mesa = FindNamed(null, "Mesa");
            _tableArea = MeasureArea(lab, mesa, new StationLayout.Area
            {
                MinX = -1f,
                MaxX = 1f,
                MinZ = -0.5f,
                MaxZ = 0.5f
            });

            if (activeGate != null)
            {
                _gateHomeScale = Mathf.Abs(activeGate.transform.localScale.x);
                if (_gateHomeScale < 0.01f)
                    _gateHomeScale = 0.25f;
                _gateUnit = ScaleSpan(MeasureSpan(activeGate.transform, lab, _gateUnit), 1f / _gateHomeScale);
            }

            if (led != null)
                _ledSpan = MeasureSpan(led.transform, lab, _ledSpan);
            if (switchA != null || switchB != null)
            {
                StationLayout.Span first = switchA != null ? MeasureSpan(switchA.transform, lab, _switchSpan) : _switchSpan;
                StationLayout.Span second = switchB != null ? MeasureSpan(switchB.transform, lab, _switchSpan) : first;
                _switchSpan = StationLayout.Span.Union(first, second);
            }

            _metricsReady = true;
        }

        private void ApplyBoardSize(StationLayout.Area used)
        {
            if (_placa == null)
                return;

            float homeWidth = _boardHome.MaxX - _boardHome.MinX;
            float homeDepth = _boardHome.MaxZ - _boardHome.MinZ;
            float usedWidth = used.MaxX - used.MinX;
            float usedDepth = used.MaxZ - used.MinZ;
            Vector3 scale = _placaHomeScale;
            if (homeWidth > 0.01f)
                scale.x = _placaHomeScale.x * (usedWidth / homeWidth);
            if (homeDepth > 0.01f)
                scale.z = _placaHomeScale.z * (usedDepth / homeDepth);
            _placa.localScale = scale;
        }

        private static StationLayout.Area MeasureArea(Transform space, Transform source, StationLayout.Area fallback)
        {
            if (source == null)
                return fallback;

            Renderer renderer = source.GetComponent<Renderer>();
            if (renderer == null)
                renderer = source.GetComponentInChildren<Renderer>();
            if (renderer == null || space == null)
                return fallback;

            Bounds bounds = renderer.bounds;
            Vector3 min = space.InverseTransformPoint(bounds.min);
            Vector3 max = space.InverseTransformPoint(bounds.max);
            return new StationLayout.Area
            {
                MinX = Mathf.Min(min.x, max.x),
                MaxX = Mathf.Max(min.x, max.x),
                MinZ = Mathf.Min(min.z, max.z),
                MaxZ = Mathf.Max(min.z, max.z)
            };
        }

        private static StationLayout.Span MeasureSpan(Transform piece, Transform space, StationLayout.Span fallback)
        {
            if (piece == null || space == null)
                return fallback;

            Vector3 origin = space.InverseTransformPoint(piece.position);
            bool any = false;
            float minX = 0f;
            float maxX = 0f;
            float minZ = 0f;
            float maxZ = 0f;
            Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;
                IncludeBounds(space, renderer.bounds, ref any, ref minX, ref maxX, ref minZ, ref maxZ);
            }

            Collider[] colliders = piece.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;
                IncludeBounds(space, collider.bounds, ref any, ref minX, ref maxX, ref minZ, ref maxZ);
            }

            if (!any)
                return fallback;

            return new StationLayout.Span
            {
                Left = Mathf.Max(0.01f, origin.x - minX),
                Right = Mathf.Max(0.01f, maxX - origin.x),
                Near = Mathf.Max(0.01f, origin.z - minZ),
                Far = Mathf.Max(0.01f, maxZ - origin.z)
            };
        }

        private static void IncludeBounds(
            Transform space,
            Bounds bounds,
            ref bool any,
            ref float minX,
            ref float maxX,
            ref float minZ,
            ref float maxZ)
        {
            Vector3 center = bounds.center;
            Vector3 extent = bounds.extents;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 world = center + new Vector3(
                    (corner & 1) == 0 ? -extent.x : extent.x,
                    (corner & 2) == 0 ? -extent.y : extent.y,
                    (corner & 4) == 0 ? -extent.z : extent.z);
                Vector3 local = space.InverseTransformPoint(world);
                if (!any)
                {
                    minX = maxX = local.x;
                    minZ = maxZ = local.z;
                    any = true;
                    continue;
                }

                if (local.x < minX)
                    minX = local.x;
                if (local.x > maxX)
                    maxX = local.x;
                if (local.z < minZ)
                    minZ = local.z;
                if (local.z > maxZ)
                    maxZ = local.z;
            }
        }

        private static StationLayout.Span ScaleSpan(StationLayout.Span span, float factor)
        {
            if (factor < 0f)
                factor = 0f;
            return new StationLayout.Span
            {
                Left = span.Left * factor,
                Right = span.Right * factor,
                Near = span.Near * factor,
                Far = span.Far * factor
            };
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root != null)
            {
                if (root.name == name)
                    return root;
                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null && children[i].name == name)
                        return children[i];
                }
            }

            GameObject found = GameObject.Find(name);
            return found != null ? found.transform : null;
        }

        private void ApplyPreset(PracticeExercise exercise)
        {
            StationPreset preset = exercise.Preset;
            if (preset == null || connector == null)
                return;

            if (exercise.Structure == null)
            {
                GateView gate = _modules.Count > 0 ? _modules[0] : activeGate;
                if (gate == null)
                    return;

                if (preset.WireSwitches && !preset.Omits("m1", "A"))
                    connector.TryWire(OutputOf(switchA), InputOf(gate, false));
                if (preset.WireSwitches && !preset.Omits("m1", "B") && gate.Node != null && gate.Node.RequiredInputCount > 1)
                    connector.TryWire(OutputOf(switchB), InputOf(gate, true));
                if (preset.WireLed)
                    connector.TryWire(OutputOf(gate), InputOfLed());
                return;
            }

            for (int i = 0; i < exercise.Structure.Links.Count; i++)
            {
                CircuitLinkSpec link = exercise.Structure.Links[i];
                bool toLed = string.Equals(link.ToId, CircuitLinkSpec.Led, StringComparison.OrdinalIgnoreCase);
                if (toLed && !preset.WireLed)
                    continue;
                if (!toLed && preset.Omits(link.ToId, link.Port))
                    continue;

                bool fromSwitch = CircuitStructure.IsSwitch(link.FromId);
                if (fromSwitch && !preset.WireSwitches)
                    continue;
                if (!fromSwitch && !toLed && !preset.WireGates)
                    continue;

                OutputPinView output = fromSwitch
                    ? OutputOf(string.Equals(link.FromId, CircuitLinkSpec.SwitchB, StringComparison.OrdinalIgnoreCase) ? switchB : switchA)
                    : OutputOf(ModuleById(link.FromId));
                InputPinView input = toLed
                    ? InputOfLed()
                    : InputOf(ModuleById(link.ToId), string.Equals(link.Port, "B", StringComparison.OrdinalIgnoreCase));
                connector.TryWire(output, input);
            }
        }

        private GateView ModuleById(string id)
        {
            if (id != null && id.Length > 1 && id[0] == 'm')
            {
                int number;
                if (int.TryParse(id.Substring(1), out number) && number >= 1 && number <= _modules.Count)
                    return _modules[number - 1];
            }

            return _modules.Count > 0 ? _modules[0] : activeGate;
        }

        private static OutputPinView OutputOf(Component owner)
        {
            if (owner == null)
                return null;

            return owner.GetComponentInChildren<OutputPinView>(true);
        }

        private static InputPinView InputOf(GateView gate, bool portB)
        {
            if (gate == null)
                return null;

            InputPinView[] pins = gate.GetComponentsInChildren<InputPinView>(true);
            for (int i = 0; i < pins.Length; i++)
            {
                InputPinView pin = pins[i];
                if (pin != null && !pin.IsLedInput && pin.IsPortB == portB)
                    return pin;
            }

            return null;
        }

        private InputPinView InputOfLed()
        {
            if (led == null)
                return null;

            return led.GetComponentInChildren<InputPinView>(true);
        }

        private void RefreshModuleButtons()
        {
            if (_tasks == null)
                return;

            int focus = 0;
            var captions = new List<string>();
            for (int i = 0; i < _modules.Count; i++)
            {
                GateView module = _modules[i];
                if (module == null || !module.gameObject.activeInHierarchy)
                    continue;

                if (module == _focused)
                    focus = captions.Count;

                string gateName = module.Node != null ? module.Node.GateName : module.Type.ToString();
                captions.Add("Comp. " + (captions.Count + 1) + "\n" + gateName);
            }

            _tasks.SetStationModules(captions, focus, FocusModule);
        }

        private void RefreshFocusVisuals()
        {
            for (int i = 0; i < _modules.Count; i++)
            {
                GateView module = _modules[i];
                if (module == null || !module.gameObject.activeInHierarchy)
                    continue;

                module.SetStationFocus(module == _focused);
            }

            HighlightSelectedGate();
        }

        private void ListenNav(string moduleName, UnityEngine.Events.UnityAction action)
        {
            Button button = _tasks != null ? _tasks.NavButton(moduleName) : null;
            if (button != null)
                button.onClick.AddListener(action);
        }

        private void SilenceNav(string moduleName, UnityEngine.Events.UnityAction action)
        {
            Button button = _tasks != null ? _tasks.NavButton(moduleName) : null;
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        private void HighlightNav(string moduleName)
        {
            Paint(_tasks != null ? _tasks.NavButton("Fundamentos") : null, moduleName == "Fundamentos");
            Paint(_tasks != null ? _tasks.NavButton("Aplicación Real") : null, moduleName == "Aplicación Real");
            Paint(_tasks != null ? _tasks.NavButton("Diagnóstico") : null, moduleName == "Diagnóstico");
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
