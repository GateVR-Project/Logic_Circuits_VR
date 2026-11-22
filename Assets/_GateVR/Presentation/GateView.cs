using TMPro;
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
    /// Estación central. Crea el nodo en Awake y puede sustituirlo durante Play
    /// sin conservar suscriptores del nodo anterior.
    /// </summary>
    public class GateView : MonoBehaviour
    {
        [SerializeField] private GateType gateType = GateType.AND;

        private GateNode _node;
        private MaterialPropertyBlock _housingTint;

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

        private void Start()
        {
            RefreshAppearance();
        }

        /// <summary>
        /// Sustituye el nodo lógico. Conserva las entradas que el tipo nuevo admite
        /// y suelta la anterior de la lista de suscriptores.
        /// </summary>
        public bool TryChangeType(GateType type)
        {
            if (_node != null && gateType == type)
            {
                RefreshAppearance();
                return false;
            }

            ISignalSource keptA = _node != null ? _node.InputA : null;
            ISignalSource keptB = _node != null ? _node.InputB : null;
            if (_node != null)
                _node.DisconnectAll();

            gateType = type;
            _node = new GateNode(CreateGate(type));

            if (keptA != null)
                _node.TryConnectInputA(keptA);

            if (_node.RequiredInputCount > 1 && keptB != null)
                _node.TryConnectInputB(keptB);

            RefreshAppearance();
            return true;
        }

        public void RefreshAppearance()
        {
            ApplyPortState();
            string gateName = _node != null ? _node.GateName : gateType.ToString();
            bool binary = _node == null || _node.RequiredInputCount > 1;
            bool foundTypeLabel = false;

            TextMeshPro[] labels = GetComponentsInChildren<TextMeshPro>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                TextMeshPro label = labels[i];
                if (label == null)
                    continue;

                string objectName = label.gameObject.name;
                if (objectName == "Label_B")
                {
                    label.gameObject.SetActive(binary);
                    continue;
                }

                if (!IsTypeLabel(objectName))
                    continue;

                label.text = gateName;
                label.gameObject.SetActive(true);
                foundTypeLabel = true;
            }

            if (!foundTypeLabel)
                CreateTypeLabel(gateName);

            TintHousing(ColorFor(gateType));
        }

        private void ApplyPortState()
        {
            bool binary = _node == null || _node.RequiredInputCount > 1;
            InputPinView[] pins = GetComponentsInChildren<InputPinView>(true);
            for (int i = 0; i < pins.Length; i++)
            {
                InputPinView pin = pins[i];
                if (pin == null || pin.IsLedInput || pin.GateOwner != this || !pin.IsPortB)
                    continue;

                pin.SetAcceptsConnection(binary);
            }
        }

        private void CreateTypeLabel(string gateName)
        {
            Transform existing = transform.Find("GateTypeLabel");
            TextMeshPro label;
            if (existing != null)
            {
                label = existing.GetComponent<TextMeshPro>();
            }
            else
            {
                GameObject plate = new GameObject("GateTypeLabel");
                plate.transform.SetParent(transform, false);
                plate.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                plate.transform.localRotation = Quaternion.identity;
                plate.transform.localScale = Vector3.one;
                label = plate.AddComponent<TextMeshPro>();
                label.fontSize = 3.2f;
                label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.sizeDelta = new Vector2(1.4f, 0.5f);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                if (TMP_Settings.defaultFontAsset != null)
                    label.font = TMP_Settings.defaultFontAsset;
            }

            if (label == null)
                return;

            label.text = gateName;
            label.color = Color.white;
            FaceUser(label.transform);
        }

        private static void FaceUser(Transform plate)
        {
            Camera camera = Camera.main;
            Vector3 user = camera != null
                ? camera.transform.position
                : plate.position + new Vector3(0f, 0.85f, -1.1f);
            Vector3 toUser = user - plate.position;
            if (toUser.sqrMagnitude < 0.0001f)
                return;

            plate.rotation = Quaternion.LookRotation(toUser, Vector3.up);
        }

        private void TintHousing(Color color)
        {
            Transform capsule = transform.Find("Visuals/Capsula");
            if (capsule == null)
                return;

            Renderer renderer = capsule.GetComponent<Renderer>();
            if (renderer == null)
                return;

            if (_housingTint == null)
                _housingTint = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(_housingTint);
            _housingTint.SetColor("_BaseColor", color);
            _housingTint.SetColor("_Color", color);
            renderer.SetPropertyBlock(_housingTint);
        }

        private static bool IsTypeLabel(string objectName)
        {
            return objectName == "GateTypeLabel"
                || objectName == "Label_AND"
                || objectName == "Label_OR"
                || objectName == "Label_NOT";
        }

        private static Color ColorFor(GateType type)
        {
            switch (type)
            {
                case GateType.OR:
                    return new Color(0.78f, 0.46f, 0.16f);
                case GateType.NOT:
                    return new Color(0.52f, 0.28f, 0.72f);
                default:
                    return new Color(0.22f, 0.42f, 0.74f);
            }
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
