using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    public class XorGate : ILogicGate
    {
        public string Name => "XOR";

        public bool Evaluate(bool[] inputs)
        {
            if (inputs == null || inputs.Length < 2)
                return false;

            return inputs[0] != inputs[1];
        }
    }
}
