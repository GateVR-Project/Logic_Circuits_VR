using UnityEngine;
using UnityEngine.InputSystem;

public class DemoShortcutController : MonoBehaviour
{
    public MonoBehaviour switchA;
    public MonoBehaviour switchB;
    public MonoBehaviour switchAPin;
    public MonoBehaviour switchBPin;
    public MonoBehaviour gateInputA;
    public MonoBehaviour gateInputB;
    public MonoBehaviour gateOutput;
    public MonoBehaviour ledInput;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Tecla 1: Conecta los 3 cables automáticamente
        if (kb.digit1Key.wasPressedThisFrame)
        {
            if (switchAPin) switchAPin.Invoke("SelectPin", 0f);
            if (gateInputA) gateInputA.Invoke("SelectPin", 0f);

            if (switchBPin) switchBPin.Invoke("SelectPin", 0f);
            if (gateInputB) gateInputB.Invoke("SelectPin", 0f);

            if (gateOutput) gateOutput.Invoke("SelectPin", 0f);
            if (ledInput) ledInput.Invoke("SelectPin", 0f);
        }

        // Tecla 2: Alterna Switch A
        if (kb.digit2Key.wasPressedThisFrame && switchA)
            switchA.Invoke("Interact", 0f);

        // Tecla 3: Alterna Switch B
        if (kb.digit3Key.wasPressedThisFrame && switchB)
            switchB.Invoke("Interact", 0f);
    }
}