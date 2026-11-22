using System.Collections.Generic;
using GateVR.Domain.Entities;
using GateVR.Domain.Interfaces;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
#endif

namespace GateVR.Presentation
{
    /// <summary>
    /// Une dos toques: primero una salida, después una entrada.
    /// Si el puerto está ocupado, reemplaza el cable. Si el motor rechaza el lazo, conserva el anterior.
    /// </summary>
    public class PinConnectorManager : MonoBehaviour
    {
        [SerializeField] private WireView wirePrefab;
        [SerializeField] private Transform wireRoot;
        [SerializeField] private TextMeshProUGUI feedbackText;

        private readonly List<WireView> _wires = new List<WireView>();
        private OutputPinView _pendingOutput;
        private bool _warnedMissingPrefab;

        public void SelectOutput(OutputPinView pin)
        {
            if (pin == null)
                return;

            if (_pendingOutput == pin)
            {
                ClearSelection();
                ShowMessage("Selección de salida cancelada.");
                return;
            }

            ClearSelection();
            _pendingOutput = pin;
            pin.SetSelected(true);
            Debug.Log("Salida seleccionada: " + pin.name + ". Toca un pin de entrada.", pin);
            ShowMessage("Salida seleccionada: " + pin.name + ". Toca un pin de entrada.");
        }

        public void SelectInput(InputPinView pin)
        {
            if (pin == null)
                return;

            if (_pendingOutput == null)
            {
                ShowMessage("Selecciona primero un pin de salida.");
                return;
            }

            OutputPinView output = _pendingOutput;
            ClearSelection();
            Connect(output, pin);
        }

        public void ClearAllWires()
        {
            for (int i = _wires.Count - 1; i >= 0; i--)
                ReleaseWire(_wires[i]);

            _wires.Clear();
            ClearSelection();
        }

        public void ShowMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            Debug.Log(message, this);
            if (feedbackText != null)
                feedbackText.text = message;
        }

        private void LateUpdate()
        {
            PollGameViewClick();
        }

        private void Connect(OutputPinView output, InputPinView input)
        {
            ISignalSource source = output.Source;
            if (source == null)
            {
                ShowMessage("Ese pin de salida no tiene una señal asignada.");
                return;
            }

            bool occupiedByOther = IsOccupiedByOther(input, source);
            if (!occupiedByOther && FindWire(input) != null)
            {
                ShowMessage("Ese cable ya está conectado.");
                return;
            }

            var result = ConnectNode(input, source, occupiedByOther);
            if (!result.IsValid)
            {
                Debug.LogWarning(result.ErrorMessage, input);
                ShowMessage(result.ErrorMessage);
                return;
            }

            RemoveWiresAt(input);
            WireView wire = CreateWire();
            wire.Bind(output, input);
            _wires.Add(wire);
            ShowMessage(occupiedByOther ? "Cable reemplazado." : "Cable conectado.");
        }

        private static (bool IsValid, string ErrorMessage) ConnectNode(
            InputPinView input,
            ISignalSource source,
            bool replaceExisting)
        {
            if (input.IsLedInput)
            {
                if (input.LedOwner == null || input.LedOwner.Node == null)
                    return (false, "El pin del LED no tiene un LedView.");

                if (replaceExisting)
                    return input.LedOwner.Node.ReplaceInput(source);

                return input.LedOwner.Node.TryConnect(source);
            }

            if (input.GateOwner == null || input.GateOwner.Node == null)
                return (false, "El pin de entrada no tiene una compuerta asignada.");

            GateNode gate = input.GateOwner.Node;
            if (input.IsPortB)
            {
                if (replaceExisting)
                    return gate.ReplaceInputB(source);

                return gate.TryConnectInputB(source);
            }

            if (replaceExisting)
                return gate.ReplaceInputA(source);

            return gate.TryConnectInputA(source);
        }

        private static bool IsOccupiedByOther(InputPinView input, ISignalSource source)
        {
            ISignalSource current = null;
            if (input.IsLedInput)
            {
                if (input.LedOwner != null && input.LedOwner.Node != null)
                    current = input.LedOwner.Node.Input;
            }
            else if (input.GateOwner != null && input.GateOwner.Node != null)
            {
                current = input.IsPortB ? input.GateOwner.Node.InputB : input.GateOwner.Node.InputA;
            }

            return current != null && !ReferenceEquals(current, source);
        }

        private WireView FindWire(InputPinView input)
        {
            for (int i = 0; i < _wires.Count; i++)
            {
                WireView wire = _wires[i];
                if (wire != null && wire.Input == input)
                    return wire;
            }

            return null;
        }

        private void RemoveWiresAt(InputPinView input)
        {
            for (int i = _wires.Count - 1; i >= 0; i--)
            {
                WireView wire = _wires[i];
                if (wire == null || wire.Input != input)
                    continue;

                ReleaseWire(wire);
                _wires.RemoveAt(i);
            }
        }

        private static void ReleaseWire(WireView wire)
        {
            if (wire == null)
                return;

            wire.Unbind();
            wire.gameObject.SetActive(false);
            Destroy(wire.gameObject);
        }

        private WireView CreateWire()
        {
            if (wirePrefab != null)
                return Instantiate(wirePrefab, wireRoot);

            if (!_warnedMissingPrefab)
            {
                _warnedMissingPrefab = true;
                Debug.LogWarning("PinConnectorManager no tiene un prefab de WireView. Se dibuja un cable temporal.", this);
            }

            GameObject wireObject = new GameObject("Wire");
            if (wireRoot != null)
                wireObject.transform.SetParent(wireRoot, false);

            LineRenderer line = wireObject.AddComponent<LineRenderer>();
            line.positionCount = 3;
            line.useWorldSpace = true;
            line.startWidth = 0.012f;
            line.endWidth = 0.012f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                line.sharedMaterial = new Material(shader);

            return wireObject.AddComponent<WireView>();
        }

        private void ClearSelection()
        {
            if (_pendingOutput != null)
                _pendingOutput.SetSelected(false);

            _pendingOutput = null;
        }

        private void PollGameViewClick()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            if (IsPointerOverUi())
                return;

            Camera camera = Camera.main;
            if (camera == null)
                return;

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                return;

            InputPinView inputPin = hit.collider.GetComponentInParent<InputPinView>();
            if (inputPin != null)
            {
                inputPin.SelectPin();
                return;
            }

            OutputPinView outputPin = hit.collider.GetComponentInParent<OutputPinView>();
            if (outputPin != null)
            {
                outputPin.SelectPin();
                return;
            }

            SwitchView switchView = hit.collider.GetComponentInParent<SwitchView>();
            if (switchView != null)
                switchView.Interact();
#endif
        }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private static bool IsPointerOverUi()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            if (eventSystem.IsPointerOverGameObject(-1))
                return true;

            return eventSystem.IsPointerOverGameObject();
        }
#endif
    }
}
