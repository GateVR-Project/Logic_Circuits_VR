using System.Linq;
using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    public class AndGate : ILogicGate
    {
        public string Name => "AND";

        public bool Evaluate(bool[] inputs)
        {
            // Si no hay cables conectados, la compuerta no emite energía
            if (inputs == null || inputs.Length == 0)
                return false;

            // La compuerta AND requiere que TODAS sus entradas sean verdaderas (true)
            return inputs.All(input => input);
        }
    }
}