using System;
using System.Collections.Generic;
using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    /// <summary>
    /// Nodo que evalúa una <see cref="ILogicGate"/> y propaga la salida
    /// cuando cambia cualquiera de sus entradas.
    /// </summary>
    public class GateNode : ISignalSource
    {
        private readonly ILogicGate _gate;
        private bool _publishedOutput;

        public GateNode(ILogicGate gate)
        {
            if (gate == null)
                throw new ArgumentNullException(nameof(gate));

            _gate = gate;
            RequiredInputCount = RequiresSingleInput(gate) ? 1 : 2;
        }

        public ILogicGate Gate
        {
            get { return _gate; }
        }

        public string GateName
        {
            get { return _gate.Name; }
        }

        /// <summary>
        /// 1 para NOT. AND, OR, NAND, NOR, XOR y XNOR piden dos entradas.
        /// </summary>
        public int RequiredInputCount { get; private set; }

        public ISignalSource InputA { get; private set; }

        public ISignalSource InputB { get; private set; }

        public bool HasAllInputsConnected
        {
            get
            {
                if (InputA == null)
                    return false;

                if (RequiredInputCount == 1)
                    return true;

                return InputB != null;
            }
        }

        public event Action OnSignalChanged;

        public bool GetSignal()
        {
            if (!HasAllInputsConnected)
                return false;

            if (RequiredInputCount == 1)
                return _gate.Evaluate(new[] { InputA.GetSignal() });

            return _gate.Evaluate(new[] { InputA.GetSignal(), InputB.GetSignal() });
        }

        /// <summary>
        /// Conecta la entrada A. Si el puerto ya tiene otro cable, no lo reemplaza.
        /// </summary>
        public (bool IsValid, string ErrorMessage) TryConnectInputA(ISignalSource source)
        {
            return Connect(isPortB: false, source: source, replaceExisting: false);
        }

        /// <summary>
        /// Conecta la entrada B. Si el puerto ya tiene otro cable, no lo reemplaza.
        /// </summary>
        public (bool IsValid, string ErrorMessage) TryConnectInputB(ISignalSource source)
        {
            return Connect(isPortB: true, source: source, replaceExisting: false);
        }

        /// <summary>
        /// Sustituye el cable de la entrada A. La fuente anterior deja de notificar a este nodo.
        /// </summary>
        public (bool IsValid, string ErrorMessage) ReplaceInputA(ISignalSource source)
        {
            return Connect(isPortB: false, source: source, replaceExisting: true);
        }

        /// <summary>
        /// Sustituye el cable de la entrada B. La fuente anterior deja de notificar a este nodo.
        /// </summary>
        public (bool IsValid, string ErrorMessage) ReplaceInputB(ISignalSource source)
        {
            return Connect(isPortB: true, source: source, replaceExisting: true);
        }

        public void DisconnectInputA()
        {
            if (InputA == null)
                return;

            InputA.OnSignalChanged -= OnInputAChanged;
            InputA = null;
            PublishIfOutputChanged();
        }

        public void DisconnectInputB()
        {
            if (InputB == null)
                return;

            InputB.OnSignalChanged -= OnInputBChanged;
            InputB = null;
            PublishIfOutputChanged();
        }

        public void DisconnectAll()
        {
            DisconnectInputA();
            DisconnectInputB();
        }

        private (bool IsValid, string ErrorMessage) Connect(bool isPortB, ISignalSource source, bool replaceExisting)
        {
            string portName = isPortB ? "B" : "A";

            if (source == null)
                return (false, "No hay una salida válida para conectar en la entrada " + portName + ".");

            if (isPortB && RequiredInputCount < 2)
                return (false, "Esta compuerta solo admite la entrada A.");

            ISignalSource current = isPortB ? InputB : InputA;
            if (current != null && !replaceExisting && !ReferenceEquals(current, source))
                return (false, "La entrada " + portName + " ya tiene un cable conectado.");

            if (ReferenceEquals(source, this))
                return (false, "No se puede conectar la salida de la compuerta a su propia entrada.");

            if (SourceDependsOnThis(source))
                return (false, "No se puede crear un ciclo: esa salida ya depende de esta compuerta.");

            if (ReferenceEquals(current, source))
                return (true, string.Empty);

            if (current != null)
            {
                if (isPortB)
                    current.OnSignalChanged -= OnInputBChanged;
                else
                    current.OnSignalChanged -= OnInputAChanged;
            }

            if (isPortB)
            {
                InputB = source;
                source.OnSignalChanged += OnInputBChanged;
            }
            else
            {
                InputA = source;
                source.OnSignalChanged += OnInputAChanged;
            }

            PublishIfOutputChanged();
            return (true, string.Empty);
        }

        private void OnInputAChanged()
        {
            PublishIfOutputChanged();
        }

        private void OnInputBChanged()
        {
            PublishIfOutputChanged();
        }

        private void PublishIfOutputChanged()
        {
            bool output = GetSignal();
            if (output == _publishedOutput)
                return;

            _publishedOutput = output;
            Action handler = OnSignalChanged;
            if (handler != null)
                handler.Invoke();
        }

        private bool SourceDependsOnThis(ISignalSource source)
        {
            return DependsOn(source, new HashSet<ISignalSource>());
        }

        /// <summary>
        /// Recorre las entradas aguas arriba. Si esta compuerta aparece, el cable cerraría un ciclo.
        /// </summary>
        private bool DependsOn(ISignalSource node, HashSet<ISignalSource> visited)
        {
            if (node == null || !visited.Add(node))
                return false;

            if (ReferenceEquals(node, this))
                return true;

            GateNode upstream = node as GateNode;
            if (upstream == null)
                return false;

            if (DependsOn(upstream.InputA, visited))
                return true;

            return DependsOn(upstream.InputB, visited);
        }

        private static bool RequiresSingleInput(ILogicGate gate)
        {
            if (gate is NotGate)
                return true;

            return string.Equals(gate.Name, "NOT", StringComparison.OrdinalIgnoreCase);
        }
    }
}
