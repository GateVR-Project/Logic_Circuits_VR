using System;
using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    /// <summary>
    /// Indicador de salida. Enciende o apaga <see cref="IsLit"/> según la señal de su entrada.
    /// </summary>
    public class LedNode
    {
        private bool _hasSample;

        public ISignalSource Input { get; private set; }

        public bool IsLit { get; private set; }

        public event Action<bool> OnLitStateChanged;

        /// <summary>
        /// Conecta la única entrada del LED. Si ya hay un cable distinto, no lo reemplaza.
        /// </summary>
        public (bool IsValid, string ErrorMessage) TryConnect(ISignalSource source)
        {
            return Connect(source, replaceExisting: false);
        }

        /// <summary>
        /// Sustituye el cable del LED y deja de escuchar a la fuente anterior.
        /// </summary>
        public (bool IsValid, string ErrorMessage) ReplaceInput(ISignalSource source)
        {
            return Connect(source, replaceExisting: true);
        }

        public void Disconnect()
        {
            if (Input == null)
                return;

            Input.OnSignalChanged -= OnInputChanged;
            Input = null;
            Publish(false);
        }

        private (bool IsValid, string ErrorMessage) Connect(ISignalSource source, bool replaceExisting)
        {
            if (source == null)
                return (false, "No hay una salida válida para conectar al LED.");

            if (Input != null && !replaceExisting && !ReferenceEquals(Input, source))
                return (false, "El LED ya tiene un cable conectado.");

            if (ReferenceEquals(Input, source))
            {
                Publish(source.GetSignal());
                return (true, string.Empty);
            }

            if (Input != null)
                Input.OnSignalChanged -= OnInputChanged;

            Input = source;
            source.OnSignalChanged += OnInputChanged;
            Publish(source.GetSignal());
            return (true, string.Empty);
        }

        private void OnInputChanged()
        {
            bool signal = Input != null && Input.GetSignal();
            Publish(signal);
        }

        private void Publish(bool lit)
        {
            if (_hasSample && IsLit == lit)
                return;

            IsLit = lit;
            _hasSample = true;

            Action<bool> handler = OnLitStateChanged;
            if (handler != null)
                handler.Invoke(IsLit);
        }
    }
}
