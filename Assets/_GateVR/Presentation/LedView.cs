using UnityEngine;
using GateVR.Domain.Entities;

namespace GateVR.Presentation
{
    /// <summary>
    /// Bombilla del circuito. El material sigue a <see cref="LedNode.OnLitStateChanged"/>.
    /// </summary>
    public class LedView : MonoBehaviour
    {
        [SerializeField] private MeshRenderer bulbRenderer;
        [SerializeField] private Material offMaterial;
        [SerializeField] private Material onMaterial;

        private LedNode _node;
        private MaterialPropertyBlock _tint;

        public LedNode Node
        {
            get { return _node; }
        }

        private void Awake()
        {
            _node = new LedNode();
            if (bulbRenderer == null)
                bulbRenderer = GetComponent<MeshRenderer>();

            _node.OnLitStateChanged += HandleLitChanged;
            RefreshVisual();
        }

        private void OnDestroy()
        {
            if (_node != null)
                _node.OnLitStateChanged -= HandleLitChanged;
        }

        public void RefreshVisual()
        {
            if (bulbRenderer == null || _node == null)
                return;

            if (offMaterial != null && onMaterial != null)
            {
                bulbRenderer.SetPropertyBlock(null);
                bulbRenderer.sharedMaterial = _node.IsLit ? onMaterial : offMaterial;
                return;
            }

            ApplyTint(_node.IsLit ? new Color(0.2f, 1f, 0.35f) : new Color(0.05f, 0.12f, 0.05f));
        }

        private void HandleLitChanged(bool isLit)
        {
            if (_node != null && _node.IsLit == isLit)
                RefreshVisual();
        }

        private void ApplyTint(Color color)
        {
            if (_tint == null)
                _tint = new MaterialPropertyBlock();

            bulbRenderer.GetPropertyBlock(_tint);
            _tint.SetColor("_BaseColor", color);
            _tint.SetColor("_Color", color);
            _tint.SetColor("_EmissionColor", _node.IsLit ? color : Color.black);
            bulbRenderer.SetPropertyBlock(_tint);
        }
    }
}
