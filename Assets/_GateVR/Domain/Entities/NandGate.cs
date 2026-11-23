using GateVR.Domain.Interfaces;

namespace GateVR.Domain.Entities
{
    public class NandGate : ILogicGate
    {
        public string Name => "NAND";

        public bool Evaluate(bool[] inputs)
        {
            if (inputs == null || inputs.Length < 2)
                return false;

            return !(inputs[0] && inputs[1]);
        }
    }
}
