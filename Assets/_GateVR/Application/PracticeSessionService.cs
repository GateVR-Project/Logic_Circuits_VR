using System;
using System.Collections.Generic;
using GateVR.Domain.Entities;
using GateVR.Domain.Interfaces;

namespace GateVR.Application
{
    /// <summary>
    /// Sesión de práctica. Valida la estructura del circuito y después su tabla de verdad.
    /// </summary>
    public class PracticeSessionService
    {
        private readonly IReadOnlyList<PracticeExercise> _exercises;

        public PracticeSessionService()
        {
            _exercises = new List<PracticeExercise>
            {
                new PracticeExercise(
                    id: "and-01",
                    title: "Compuerta AND",
                    objective: "Enciende el LED solo cuando los dos interruptores están encendidos.",
                    logicConditionText: "LED = A AND B",
                    requiredGateType: "AND",
                    expectedTruthTable: (a, b) => a && b),
                new PracticeExercise(
                    id: "or-01",
                    title: "Compuerta OR",
                    objective: "Enciende el LED cuando al menos un interruptor está encendido.",
                    logicConditionText: "LED = A OR B",
                    requiredGateType: "OR",
                    expectedTruthTable: (a, b) => a || b)
            }.AsReadOnly();

            CurrentExercise = _exercises[0];
        }

        public IReadOnlyList<PracticeExercise> Exercises
        {
            get { return _exercises; }
        }

        public PracticeExercise CurrentExercise { get; private set; }

        public void SelectExercise(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El id del ejercicio es obligatorio.", nameof(id));

            for (int i = 0; i < _exercises.Count; i++)
            {
                if (string.Equals(_exercises[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    CurrentExercise = _exercises[i];
                    return;
                }
            }

            throw new ArgumentException("No existe un ejercicio con id '" + id + "'.", nameof(id));
        }

        /// <summary>
        /// Comprueba el cableado y, si es válido, recorre las cuatro combinaciones de A y B.
        /// Restaura el estado original de los interruptores antes de volver.
        /// </summary>
        public (bool IsSuccess, string FeedbackMessage) ValidateCurrentCircuit(
            SwitchNode switchA,
            SwitchNode switchB,
            GateNode targetGate,
            LedNode led)
        {
            if (switchA == null)
                throw new ArgumentNullException(nameof(switchA));
            if (switchB == null)
                throw new ArgumentNullException(nameof(switchB));
            if (targetGate == null)
                throw new ArgumentNullException(nameof(targetGate));
            if (led == null)
                throw new ArgumentNullException(nameof(led));

            var structural = ValidateStructure(switchA, switchB, targetGate, led);
            if (!structural.IsSuccess)
                return structural;

            return EvaluateTruthTable(switchA, switchB, led);
        }

        /// <summary>
        /// Quita los cables de la compuerta y del LED, y deja ambos interruptores apagados.
        /// </summary>
        public void ResetCircuit(SwitchNode switchA, SwitchNode switchB, GateNode targetGate, LedNode led)
        {
            if (switchA == null)
                throw new ArgumentNullException(nameof(switchA));
            if (switchB == null)
                throw new ArgumentNullException(nameof(switchB));
            if (targetGate == null)
                throw new ArgumentNullException(nameof(targetGate));
            if (led == null)
                throw new ArgumentNullException(nameof(led));

            targetGate.DisconnectAll();
            led.Disconnect();
            switchA.SetState(false);
            switchB.SetState(false);
        }

        private (bool IsSuccess, string FeedbackMessage) ValidateStructure(
            SwitchNode switchA,
            SwitchNode switchB,
            GateNode targetGate,
            LedNode led)
        {
            if (ReferenceEquals(switchA, switchB))
                return (false, "Los interruptores A y B deben ser dos nodos distintos.");

            if (led.Input == null)
                return (false, "El LED no tiene una fuente conectada.");

            if (led.Input is SwitchNode)
                return (false, "El LED está conectado directamente a un interruptor. La señal debe pasar por la compuerta.");

            if (!ReferenceEquals(led.Input, targetGate))
                return (false, "El LED debe conectarse a la salida de la compuerta del ejercicio.");

            PracticeExercise exercise = CurrentExercise;
            if (!string.Equals(targetGate.GateName, exercise.RequiredGateType, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "El ejercicio requiere una compuerta " + exercise.RequiredGateType
                    + " y el nodo evaluado es " + targetGate.GateName + ".");
            }

            if (!targetGate.HasAllInputsConnected)
            {
                int connected = CountConnectedInputs(targetGate);
                return (false, "La compuerta " + targetGate.GateName + " tiene " + connected
                    + " de " + targetGate.RequiredInputCount + " entradas conectadas.");
            }

            bool reachesA = IsUpstream(targetGate, switchA, new HashSet<ISignalSource>());
            bool reachesB = IsUpstream(targetGate, switchB, new HashSet<ISignalSource>());
            if (!reachesA && !reachesB)
                return (false, "La compuerta no recibe la señal de los interruptores A y B.");
            if (!reachesA)
                return (false, "La señal del interruptor A no llega a la compuerta.");
            if (!reachesB)
                return (false, "La señal del interruptor B no llega a la compuerta.");

            return (true, string.Empty);
        }

        private (bool IsSuccess, string FeedbackMessage) EvaluateTruthTable(
            SwitchNode switchA,
            SwitchNode switchB,
            LedNode led)
        {
            bool originalA = switchA.IsOn;
            bool originalB = switchB.IsOn;
            string failure = null;

            try
            {
                bool[] valuesA = { false, false, true, true };
                bool[] valuesB = { false, true, false, true };
                Func<bool, bool, bool> expectedOf = CurrentExercise.ExpectedTruthTable;

                for (int i = 0; i < valuesA.Length; i++)
                {
                    bool a = valuesA[i];
                    bool b = valuesB[i];
                    switchA.SetState(a);
                    switchB.SetState(b);

                    bool expected = expectedOf(a, b);
                    bool actual = led.IsLit;
                    if (actual == expected)
                        continue;

                    failure = "Falló la combinación A=" + Bit(a) + ", B=" + Bit(b)
                        + ". Se esperaba " + Bit(expected) + " y el LED mostró " + Bit(actual) + ".";
                    break;
                }
            }
            finally
            {
                switchA.SetState(originalA);
                switchB.SetState(originalB);
            }

            if (failure != null)
                return (false, failure);

            return (true, "Circuito correcto. Las cuatro combinaciones coinciden con "
                + CurrentExercise.LogicConditionText + ".");
        }

        private static int CountConnectedInputs(GateNode gate)
        {
            int connected = 0;
            if (gate.InputA != null)
                connected++;

            if (gate.RequiredInputCount > 1 && gate.InputB != null)
                connected++;

            return connected;
        }

        private static bool IsUpstream(ISignalSource node, ISignalSource target, HashSet<ISignalSource> visited)
        {
            if (node == null || !visited.Add(node))
                return false;

            if (ReferenceEquals(node, target))
                return true;

            GateNode gate = node as GateNode;
            if (gate == null)
                return false;

            if (IsUpstream(gate.InputA, target, visited))
                return true;

            return IsUpstream(gate.InputB, target, visited);
        }

        private static string Bit(bool value)
        {
            return value ? "1" : "0";
        }
    }
}
