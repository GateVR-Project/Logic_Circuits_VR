namespace GateVR.Domain.Interfaces
{
    public interface ILogicGate
    {
        /// <summary>
        /// Nombre identificador de la compuerta lógica.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Evalúa las señales de entrada y retorna el resultado determinista.
        /// </summary>
        /// <param name="inputs">Arreglo de estados booleanos de entrada (true = encendido, false = apagado).</param>
        /// <returns>Resultado de la operación lógica.</returns>
        bool Evaluate(bool[] inputs);
    }
}