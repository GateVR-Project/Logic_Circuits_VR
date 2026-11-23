using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    public class XnorGate : ILogicGate
    {
        public string Name => "XNOR";

        public bool Evaluate(bool[] inputs)
        {
            if (inputs == null || inputs.Length < 2)
                return false;

            return inputs[0] == inputs[1];
        }
    }
}
