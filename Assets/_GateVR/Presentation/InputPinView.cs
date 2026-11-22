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

        private bool _acceptsConnection = true;

        public bool AcceptsConnection
        {
            get { return _acceptsConnection; }
        }

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

        /// <summary>
        /// La entrada B de NOT deja de recibir rayos y de aceptar cables.
        /// El RayInteractable permanece en el objeto; al reactivar el collider vuelve a ser seleccionable.
        /// </summary>
        public void SetAcceptsConnection(bool accepts)
        {
            _acceptsConnection = accepts;

            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = accepts;

            Transform visuals = transform.Find("Visuals");
            if (visuals != null)
            {
                visuals.gameObject.SetActive(accepts);
                return;
            }

            Renderer[] renderers = GetComponents<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].enabled = accepts;
        }

        public void SelectPin()
        {
            if (connector == null)
            {
                Debug.LogWarning("InputPinView no encuentra un PinConnectorManager.", this);
                return;
            }

            if (!_acceptsConnection)
            {
                connector.ShowMessage("La compuerta NOT solo usa la entrada A.");
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
