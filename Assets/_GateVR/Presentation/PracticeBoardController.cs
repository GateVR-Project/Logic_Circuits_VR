using System;
using GateVR.Application;
using GateVR.Domain.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GateVR.Presentation
{
    /// <summary>
    /// Panel de la práctica. Los botones llaman al servicio de aplicación y después sincronizan las vistas.
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

        [SerializeField] private Color hintColor = Color.white;
        [SerializeField] private Color successColor = new Color(0.5f, 0.95f, 0.55f);
        [SerializeField] private Color errorColor = new Color(1f, 0.45f, 0.4f);

        private PracticeSessionService _session;

        private void Awake()
        {
            _session = new PracticeSessionService();
        }

        private void OnEnable()
        {
            if (checkButton != null)
                checkButton.onClick.AddListener(CheckCircuit);

            if (nextButton != null)
                nextButton.onClick.AddListener(SelectNextExercise);

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetPractice);
        }

        private void OnDisable()
        {
            if (checkButton != null)
                checkButton.onClick.RemoveListener(CheckCircuit);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(SelectNextExercise);

            if (resetButton != null)
                resetButton.onClick.RemoveListener(ResetPractice);
        }

        private void Start()
        {
            ShowCurrentExercise();
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
            int current = 0;
            for (int i = 0; i < _session.Exercises.Count; i++)
            {
                if (string.Equals(_session.Exercises[i].Id, _session.CurrentExercise.Id, StringComparison.Ordinal))
                {
                    current = i;
                    break;
                }
            }

            int next = (current + 1) % _session.Exercises.Count;
            _session.SelectExercise(_session.Exercises[next].Id);
            ShowCurrentExercise();
        }

        public void ResetPractice()
        {
            SwitchNode nodeA;
            SwitchNode nodeB;
            GateNode gate;
            LedNode bulb;
            if (!TryGetNodes(out nodeA, out nodeB, out gate, out bulb))
                return;

            _session.ResetCircuit(nodeA, nodeB, gate, bulb);
            if (connector != null)
                connector.ClearAllWires();

            RefreshViews();
            SetFeedback("Circuito reiniciado. Interruptores apagados y cables retirados.", hintColor);
        }

        private void ShowCurrentExercise()
        {
            PracticeExercise exercise = _session.CurrentExercise;
            if (titleText != null)
                titleText.text = exercise.Title;

            if (objectiveText != null)
                objectiveText.text = exercise.Objective;

            if (conditionText != null)
                conditionText.text = exercise.LogicConditionText;

            if (activeGate != null && activeGate.Node != null
                && !string.Equals(activeGate.Node.GateName, exercise.RequiredGateType, StringComparison.OrdinalIgnoreCase))
            {
                SetFeedback(
                    "La compuerta de la escena es " + activeGate.Node.GateName
                    + ". Este ejercicio pide " + exercise.RequiredGateType + ".",
                    errorColor);
                return;
            }

            SetFeedback("Conecta los interruptores a la compuerta y la salida de la compuerta al LED.", hintColor);
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
    }
}
