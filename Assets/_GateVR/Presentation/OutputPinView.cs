using UnityEngine;
using GateVR.Domain.Interfaces;

namespace GateVR.Presentation
{
    /// <summary>
    /// Pin de salida de un interruptor o de una compuerta.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OutputPinView : MonoBehaviour
    {
        [SerializeField] private SwitchView switchOwner;
        [SerializeField] private GateView gateOwner;
        [SerializeField] private PinConnectorManager connector;
        [SerializeField] private MeshRenderer highlightRenderer;
        [SerializeField] private Transform connectionAnchor;
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f);

        private MaterialPropertyBlock _highlight;

        public ISignalSource Source
        {
            get
            {
                if (switchOwner != null)
                    return switchOwner.Node;

                if (gateOwner != null)
                    return gateOwner.Node;

                return null;
            }
        }

        public void AssignGate(GateView gate)
        {
            gateOwner = gate;
            if (gate != null)
                switchOwner = null;
        }

        public bool BelongsTo(GateView gate)
        {
            return gate != null && gateOwner == gate;
        }

        public Vector3 ConnectionPoint
        {
            get { return PinConnection.Resolve(connectionAnchor, this); }
        }

        private void Awake()
        {
            if (connector == null)
                connector = FindAnyObjectByType<PinConnectorManager>();

            if (highlightRenderer == null)
                highlightRenderer = GetComponent<MeshRenderer>();
        }

        public void SelectPin()
        {
            if (connector == null)
            {
                Debug.LogWarning("OutputPinView no encuentra un PinConnectorManager.", this);
                return;
            }

            connector.SelectOutput(this);
        }

        private void OnMouseDown()
        {
            SelectPin();
        }

        public void SetSelected(bool selected)
        {
            if (highlightRenderer == null)
                return;

            if (!selected)
            {
                highlightRenderer.SetPropertyBlock(null);
                return;
            }

            if (_highlight == null)
                _highlight = new MaterialPropertyBlock();

            highlightRenderer.GetPropertyBlock(_highlight);
            _highlight.SetColor("_BaseColor", selectedColor);
            _highlight.SetColor("_Color", selectedColor);
            _highlight.SetColor("_EmissionColor", selectedColor);
            highlightRenderer.SetPropertyBlock(_highlight);
        }
    }
}
