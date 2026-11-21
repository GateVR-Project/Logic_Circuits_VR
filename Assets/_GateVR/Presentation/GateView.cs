using UnityEngine;
using GateVR.Domain.Entities;
using GateVR.Domain.Interfaces;

namespace GateVR.Presentation
{
    public enum GateType
    {
        AND,
        OR,
        NOT
    }

    /// <summary>
    /// Crea el <see cref="GateNode"/> de la compuerta elegida en el Inspector.
    /// El nodo queda fijo durante la sesión de Play.
    /// </summary>
    public class GateView : MonoBehaviour
    {
        [SerializeField] private GateType gateType = GateType.AND;

        private GateNode _node;

        public GateType Type
        {
            get { return gateType; }
        }

        public GateNode Node
        {
            get { return _node; }
        }

        private void Awake()
        {
            _node = new GateNode(CreateGate(gateType));
        }

        private static ILogicGate CreateGate(GateType type)
        {
            switch (type)
            {
                case GateType.OR:
                    return new OrGate();
                case GateType.NOT:
                    return new NotGate();
                default:
                    return new AndGate();
            }
        }
    }
}
