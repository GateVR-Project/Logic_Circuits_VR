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
            _exercises = PracticeCatalog.Create();
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
        /// Avanza a la práctica siguiente. No comprueba el circuito ni lo da por aprobado.
        /// La estación debe usar <see cref="PracticeExercise.InitialGateType"/>.
        /// </summary>
        public PracticeExercise AdvanceExercise()
        {
            int current = 0;
            for (int i = 0; i < _exercises.Count; i++)
            {
                if (string.Equals(_exercises[i].Id, CurrentExercise.Id, StringComparison.Ordinal))
                {
                    current = i;
                    break;
                }
            }

            int next = (current + 1) % _exercises.Count;
            CurrentExercise = _exercises[next];
            return CurrentExercise;
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
        /// Valida la placa completa. Si la práctica define una red, no basta con el chip del LED.
        /// </summary>
        public (bool IsSuccess, string FeedbackMessage) ValidateCurrentCircuit(
            SwitchNode switchA,
            SwitchNode switchB,
            IReadOnlyList<StationGate> gates,
            LedNode led)
        {
            if (switchA == null)
                throw new ArgumentNullException(nameof(switchA));
            if (switchB == null)
                throw new ArgumentNullException(nameof(switchB));
            if (led == null)
                throw new ArgumentNullException(nameof(led));
            if (gates == null || gates.Count == 0)
                throw new ArgumentException("La estación no tiene compuertas.", nameof(gates));

            if (CurrentExercise.Structure == null)
                return ValidateCurrentCircuit(switchA, switchB, gates[0].Node, led);

            var structural = ValidateNetwork(switchA, switchB, gates, led);
            if (!structural.IsSuccess)
                return structural;

            return EvaluateTruthTable(switchA, switchB, led);
        }

        /// <summary>
        /// Revisa el cableado sin recorrer la tabla de verdad ni mover los interruptores.
        /// </summary>
        public (bool IsValid, string FeedbackMessage) InspectStructure(
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

            return ValidateStructure(switchA, switchB, targetGate, led);
        }

        public (bool IsValid, string FeedbackMessage) InspectStructure(
            SwitchNode switchA,
            SwitchNode switchB,
            IReadOnlyList<StationGate> gates,
            LedNode led)
        {
            if (switchA == null)
                throw new ArgumentNullException(nameof(switchA));
            if (switchB == null)
                throw new ArgumentNullException(nameof(switchB));
            if (led == null)
                throw new ArgumentNullException(nameof(led));
            if (gates == null || gates.Count == 0)
                throw new ArgumentException("La estación no tiene compuertas.", nameof(gates));

            if (CurrentExercise.Structure == null)
                return InspectStructure(switchA, switchB, gates[0].Node, led);

            return ValidateNetwork(switchA, switchB, gates, led);
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

        public void ResetCircuit(SwitchNode switchA, SwitchNode switchB, IReadOnlyList<GateNode> gates, LedNode led)
        {
            if (switchA == null)
                throw new ArgumentNullException(nameof(switchA));
            if (switchB == null)
                throw new ArgumentNullException(nameof(switchB));
            if (led == null)
                throw new ArgumentNullException(nameof(led));
            if (gates == null || gates.Count == 0)
                throw new ArgumentException("La estación no tiene compuertas.", nameof(gates));

            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null)
                    gates[i].DisconnectAll();
            }

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

            if (exercise.UsesSingleInput)
                return ValidateSingleInput(switchA, targetGate);

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

        private (bool IsSuccess, string FeedbackMessage) ValidateNetwork(
            SwitchNode switchA,
            SwitchNode switchB,
            IReadOnlyList<StationGate> gates,
            LedNode led)
        {
            if (ReferenceEquals(switchA, switchB))
                return (false, "Los interruptores A y B deben ser dos nodos distintos.");

            var byId = new Dictionary<string, GateNode>(StringComparer.Ordinal);
            for (int i = 0; i < gates.Count; i++)
            {
                StationGate station = gates[i];
                if (station == null || station.Node == null)
                    return (false, "La estación tiene un módulo sin nodo lógico.");

                if (byId.ContainsKey(station.Id))
                    return (false, "La estación repite el módulo " + station.Id + ".");

                byId.Add(station.Id, station.Node);
            }

            CircuitStructure structure = CurrentExercise.Structure;
            for (int i = 0; i < structure.Modules.Count; i++)
            {
                CircuitModuleSpec module = structure.Modules[i];
                GateNode node;
                if (!byId.TryGetValue(module.Id, out node))
                    return (false, "Falta el módulo " + module.Id + " (" + module.GateType + ").");

                if (!string.Equals(node.GateName, module.GateType, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "El módulo " + module.Id + " debe ser " + module.GateType
                        + " y es " + node.GateName + ".");
                }
            }

            for (int i = 0; i < structure.Links.Count; i++)
            {
                CircuitLinkSpec link = structure.Links[i];
                var source = ResolveSource(link.FromId, switchA, switchB, byId);
                if (!source.Found)
                    return (false, source.Error);

                if (string.Equals(link.ToId, CircuitLinkSpec.Led, StringComparison.OrdinalIgnoreCase))
                {
                    if (led.Input == null)
                        return (false, "El LED no tiene una fuente conectada.");

                    if (led.Input is SwitchNode)
                        return (false, "El LED está conectado directamente a un interruptor. La señal debe pasar por las compuertas.");

                    if (!ReferenceEquals(led.Input, source.Node))
                        return (false, "El LED debe recibir la salida de " + link.FromId + ".");

                    continue;
                }

                GateNode target;
                if (!byId.TryGetValue(link.ToId, out target))
                    return (false, "Falta el módulo " + link.ToId + ".");

                bool portB = string.Equals(link.Port, "B", StringComparison.OrdinalIgnoreCase);
                if (portB && target.RequiredInputCount < 2)
                    return (false, "La compuerta " + target.GateName + " solo admite la entrada A.");

                ISignalSource actual = portB ? target.InputB : target.InputA;
                string portName = portB ? "B" : "A";
                if (actual == null)
                    return (false, "La entrada " + portName + " de " + link.ToId + " no tiene cable.");

                if (!ReferenceEquals(actual, source.Node))
                    return (false, "La entrada " + portName + " de " + link.ToId + " no recibe " + link.FromId + ".");
            }

            for (int i = 0; i < structure.Modules.Count; i++)
            {
                GateNode node = byId[structure.Modules[i].Id];
                if (node.RequiredInputCount < 2 && node.InputB != null)
                    return (false, "La compuerta " + node.GateName + " solo admite la entrada A. Desconecta la entrada B.");

                if (HasCycle(node))
                    return (false, "El circuito contiene un ciclo lógico.");
            }

            return (true, string.Empty);
        }

        private static (bool Found, ISignalSource Node, string Error) ResolveSource(
            string id,
            SwitchNode switchA,
            SwitchNode switchB,
            Dictionary<string, GateNode> byId)
        {
            if (string.Equals(id, CircuitLinkSpec.SwitchA, StringComparison.OrdinalIgnoreCase))
                return (true, switchA, string.Empty);

            if (string.Equals(id, CircuitLinkSpec.SwitchB, StringComparison.OrdinalIgnoreCase))
                return (true, switchB, string.Empty);

            GateNode gate;
            if (!byId.TryGetValue(id, out gate))
                return (false, null, "Falta el módulo " + id + ".");

            return (true, gate, string.Empty);
        }

        private static bool HasCycle(GateNode origin)
        {
            return Walk(origin, new HashSet<ISignalSource>());
        }

        private static bool Walk(ISignalSource node, HashSet<ISignalSource> stack)
        {
            if (node == null)
                return false;

            if (!stack.Add(node))
                return true;

            GateNode gate = node as GateNode;
            bool cycle = false;
            if (gate != null)
                cycle = Walk(gate.InputA, stack) || Walk(gate.InputB, stack);

            stack.Remove(node);
            return cycle;
        }

        private (bool IsSuccess, string FeedbackMessage) ValidateSingleInput(SwitchNode switchA, GateNode targetGate)
        {
            if (targetGate.InputB != null)
                return (false, "La compuerta NOT solo admite la entrada A. Desconecta la entrada B.");

            if (targetGate.InputA == null)
                return (false, "La compuerta NOT tiene 0 de 1 entradas conectadas.");

            if (!IsUpstream(targetGate, switchA, new HashSet<ISignalSource>()))
                return (false, "La señal del interruptor A no llega a la compuerta.");

            return (true, string.Empty);
        }

        private (bool IsSuccess, string FeedbackMessage) EvaluateTruthTable(
            SwitchNode switchA,
            SwitchNode switchB,
            LedNode led)
        {
            if (CurrentExercise.UsesSingleInput)
                return EvaluateSingleInput(switchA, led);

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

        /// <summary>
        /// Recorre A=0 y A=1. No modifica el interruptor B.
        /// </summary>
        private (bool IsSuccess, string FeedbackMessage) EvaluateSingleInput(SwitchNode switchA, LedNode led)
        {
            bool originalA = switchA.IsOn;
            string failure = null;

            try
            {
                bool[] values = { false, true };
                for (int i = 0; i < values.Length; i++)
                {
                    bool a = values[i];
                    switchA.SetState(a);

                    bool expected = CurrentExercise.ExpectedTruthTable(a, false);
                    bool actual = led.IsLit;
                    if (actual == expected)
                        continue;

                    failure = "Falló la combinación A=" + Bit(a)
                        + ". Se esperaba " + Bit(expected) + " y el LED mostró " + Bit(actual) + ".";
                    break;
                }
            }
            finally
            {
                switchA.SetState(originalA);
            }

            if (failure != null)
                return (false, failure);

            return (true, "Circuito correcto. Las dos combinaciones coinciden con "
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
