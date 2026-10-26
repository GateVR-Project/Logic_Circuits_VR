using System.Linq;
using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    public class OrGate : ILogicGate
    {
        public string Name => "OR";

        public bool Evaluate(bool[] inputs)
        {
            if (inputs == null || inputs.Length == 0)
                return false;

            // La compuerta OR requiere que AL MENOS UNA de sus entradas sea verdadera
            return inputs.Any(input => input);
        }
    }
}