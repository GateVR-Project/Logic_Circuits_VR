using System;

namespace GateVR.Domain.Interfaces
{
    /// <summary>
    /// Fuente de una señal booleana. Los nodos aguas abajo se suscriben a
    /// <see cref="OnSignalChanged"/> para propagar el circuito.
    /// </summary>
    public interface ISignalSource
    {
        /// <summary>
        /// Señal actual. true = encendido, false = apagado.
        /// </summary>
        bool GetSignal();

        /// <summary>
        /// Se dispara cuando la señal de esta fuente cambia de valor.
        /// </summary>
        event Action OnSignalChanged;
    }
}
