using UnityEngine;

namespace GateVR.Presentation
{
    public enum GateInputPort
    {
        A,
        B
    }

    /// <summary>
    /// Pin de entrada de una compuerta (A o B) o del LED.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InputPinView : MonoBehaviour
    {
        [SerializeField] private GateView gateOwner;
        [SerializeField] private GateInputPort gatePort = GateInputPort.A;
        [SerializeField] private LedView ledOwner;
        [SerializeField] private PinConnectorManager connector;
        [SerializeField] private Transform connectionAnchor;

        public GateView GateOwner
        {
            get { return gateOwner; }
        }

        public LedView LedOwner
        {
            get { return ledOwner; }
        }

        public bool IsLedInput
        {
            get { return ledOwner != null; }
        }

        public bool IsPortB
        {
            get { return gatePort == GateInputPort.B; }
        }

        public Vector3 ConnectionPoint
        {
            get { return PinConnection.Resolve(connectionAnchor, this); }
        }

        private void Awake()
        {
            if (connector == null)
                connector = FindAnyObjectByType<PinConnectorManager>();
        }

        public void SelectPin()
        {
            if (connector == null)
            {
                Debug.LogWarning("InputPinView no encuentra un PinConnectorManager.", this);
                return;
            }

            connector.SelectInput(this);
        }

        private void OnMouseDown()
        {
            SelectPin();
        }
    }
}
