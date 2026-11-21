using UnityEngine;
using GateVR.Domain.Entities;

namespace GateVR.Presentation
{
    /// <summary>
    /// Vista de un interruptor. <see cref="Interact"/> es el punto que enlaza
    /// el ratón y los eventos WhenSelect / Poke del Interaction SDK.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SwitchView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material onMaterial;

        private SwitchNode _node;
        private MaterialPropertyBlock _tint;

        public SwitchNode Node
        {
            get { return _node; }
        }

        private void Awake()
        {
            _node = new SwitchNode();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            RefreshVisual();
        }

        private void OnEnable()
        {
            _node.OnSignalChanged += RefreshVisual;
        }

        private void OnDisable()
        {
            if (_node != null)
                _node.OnSignalChanged -= RefreshVisual;
        }

        public void Interact()
        {
            if (_node == null)
                return;

            _node.Toggle();
            RefreshVisual();
        }

        private void OnMouseDown()
        {
            Interact();
        }

        public void RefreshVisual()
        {
            if (meshRenderer == null || _node == null)
                return;

            if (offMaterial != null && onMaterial != null)
            {
                meshRenderer.SetPropertyBlock(null);
                meshRenderer.sharedMaterial = _node.IsOn ? onMaterial : offMaterial;
                return;
            }

            ApplyTint(_node.IsOn ? new Color(0.15f, 0.9f, 0.25f) : new Color(0.75f, 0.12f, 0.1f));
        }

        private void ApplyTint(Color color)
        {
            if (_tint == null)
                _tint = new MaterialPropertyBlock();

            meshRenderer.GetPropertyBlock(_tint);
            _tint.SetColor("_BaseColor", color);
            _tint.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(_tint);
        }
    }
}
