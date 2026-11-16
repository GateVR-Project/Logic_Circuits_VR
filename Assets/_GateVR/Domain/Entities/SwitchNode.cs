using System;
using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    /// <summary>
    /// Interruptor manual. La salida es el estado interno <see cref="IsOn"/>.
    /// </summary>
    public class SwitchNode : ISignalSource
    {
        public bool IsOn { get; private set; }

        public event Action OnSignalChanged;

        public bool GetSignal()
        {
            return IsOn;
        }

        /// <summary>
        /// Invierte <see cref="IsOn"/> y notifica a los nodos conectados.
        /// </summary>
        public void Toggle()
        {
            SetState(!IsOn);
        }

        /// <summary>
        /// Fija el estado. Notifica <see cref="OnSignalChanged"/> solo si el valor cambia,
        /// porque una escritura repetida no altera la señal del circuito.
        /// </summary>
        public void SetState(bool state)
        {
            if (IsOn == state)
                return;

            IsOn = state;

            Action handler = OnSignalChanged;
            if (handler != null)
                handler.Invoke();
        }
    }
}
