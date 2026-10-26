using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    public class NotGate : ILogicGate
    {
        public string Name => "NOT";

        public bool Evaluate(bool[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return false;

            // La compuerta NOT invierte la señal. Solo evalúa la primera entrada.
            return !inputs[0];
        }
    }
}