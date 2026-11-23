using UnityEngine;
using GateVR.Domain.Interfaces;

namespace GateVR.Presentation
{
    /// <summary>
    /// Cable visual. Sigue a los pines si la compuerta se mueve y copia el nivel lógico en el color.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class WireView : MonoBehaviour
    {
        [SerializeField] private Color offColor = new Color(0.22f, 0.28f, 0.38f);
        [SerializeField] private Color onColor = new Color(0.15f, 0.95f, 0.42f);

        private LineRenderer _line;
        private OutputPinView _output;
        private InputPinView _input;
        private ISignalSource _source;

        public OutputPinView Output
        {
            get { return _output; }
        }

        public InputPinView Input
        {
            get { return _input; }
        }

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 3;
            _line.useWorldSpace = true;
            if (_line.startWidth < 0.016f)
            {
                _line.startWidth = 0.018f;
                _line.endWidth = 0.018f;
            }
        }

        public void Bind(OutputPinView output, InputPinView input)
        {
            Unbind();
            _output = output;
            _input = input;
            _source = output != null ? output.Source : null;

            if (_source != null)
                _source.OnSignalChanged += RefreshColor;

            RefreshColor();
            UpdatePositions();
        }

        public void Unbind()
        {
            if (_source != null)
                _source.OnSignalChanged -= RefreshColor;

            _source = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Update()
        {
            UpdatePositions();
        }

        private void UpdatePositions()
        {
            if (_line == null || _output == null || _input == null)
                return;

            Vector3 start = _output.ConnectionPoint;
            Vector3 end = _input.ConnectionPoint;
            Vector3 mid = (start + end) * 0.5f;
            mid.y = Mathf.Max(start.y, end.y) + 0.1f;
            _line.positionCount = 3;
            _line.SetPosition(0, start);
            _line.SetPosition(1, mid);
            _line.SetPosition(2, end);
        }

        private void RefreshColor()
        {
            if (_line == null)
                return;

            bool signal = _source != null && _source.GetSignal();
            Color color = signal ? onColor : offColor;
            _line.startColor = color;
            _line.endColor = color;
        }
    }
}
