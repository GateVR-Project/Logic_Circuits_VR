using System.IO;
using System.Text;
using GateVR.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GateVR.EditorTools
{
    /// <summary>
    /// Sustituye los cubos visibles por módulos compuestos.
    /// Conserva los colliders funcionales y no toca VrPointerBinding.
    /// </summary>
    public static class LabVisualConfigurator
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string BackupPath = "Backups/SampleScene_before_visuals.unity";

        [MenuItem("GateVR/Configurar laboratorio visual")]
        public static void ConfigureActiveScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "GateVR",
                    "Sal de Play Mode antes de configurar los modelos. DestroyImmediate no se ejecuta durante la simulación.",
                    "Entendido");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            Configure(scene, true);
        }

        public static void ConfigureFromCommandLine()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("GateVR: el configurador no corre en Play Mode.");
                EditorApplication.Exit(1);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath);
            bool ok = Configure(scene, false);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Configure(Scene scene, bool interactive)
        {
            if (string.IsNullOrEmpty(scene.path) || !scene.path.Replace('\\', '/').EndsWith("Assets/Scenes/SampleScene.unity"))
                return Fail(interactive, "Abre Assets/Scenes/SampleScene.unity. El configurador no modifica otras escenas.");

            StringBuilder errors = new StringBuilder();
            SwitchView[] switches = UnityEngine.Object.FindObjectsByType<SwitchView>();
            GateView[] gates = UnityEngine.Object.FindObjectsByType<GateView>();
            LedView[] leds = UnityEngine.Object.FindObjectsByType<LedView>();
            OutputPinView[] outputs = UnityEngine.Object.FindObjectsByType<OutputPinView>();
            InputPinView[] inputs = UnityEngine.Object.FindObjectsByType<InputPinView>();
            VrPointerBinding binding = UnityEngine.Object.FindAnyObjectByType<VrPointerBinding>();

            if (switches.Length < 2)
                errors.AppendLine("Se necesitan los dos SwitchView.");
            if (gates.Length < 1)
                errors.AppendLine("Falta GateView.");
            if (leds.Length < 1)
                errors.AppendLine("Falta LedView.");
            if (outputs.Length < 3)
                errors.AppendLine("Faltan pines de salida. Encontrados: " + outputs.Length + ".");
            if (inputs.Length < 3)
                errors.AppendLine("Faltan pines de entrada. Encontrados: " + inputs.Length + ".");
            if (binding == null)
                errors.AppendLine("Falta VrPointerBinding. No se reconfigura la escena sin el enlace de rayos.");
            if (!HasSerializedProperties(switches, leds, outputs, errors))
                return Fail(interactive, errors.ToString());
            if (errors.Length > 0)
                return Fail(interactive, errors.ToString());
            if (!LabVisualLibrary.TryPrepare(errors))
                return Fail(interactive, errors.ToString());

            if (!BackupOriginalScene(errors))
                return Fail(interactive, errors.ToString());

            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configurar laboratorio visual");
            try
            {
                float support = ResolveSupport(scene);
                PlaceCanvas();
                BuildBoard(scene, support);
                for (int i = 0; i < switches.Length; i++)
                    BuildSwitch(switches[i], support, errors);
                for (int i = 0; i < gates.Length; i++)
                    BuildGate(gates[i], support, errors);
                for (int i = 0; i < leds.Length; i++)
                    BuildLed(leds[i], support, errors);
                for (int i = 0; i < outputs.Length; i++)
                    BuildOutputPin(outputs[i], errors);
                for (int i = 0; i < inputs.Length; i++)
                    BuildInputPin(inputs[i], errors);

                if (errors.Length > 0)
                    throw new System.InvalidOperationException(errors.ToString());

                if (!Audit(switches, gates, leds, outputs, inputs, binding, errors))
                    throw new System.InvalidOperationException(errors.ToString());

                Undo.CollapseUndoOperations(undo);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("GateVR: laboratorio visual configurado. Soporte en Y=" + support.ToString("0.000") + ".");
                if (interactive)
                {
                    EditorUtility.DisplayDialog(
                        "GateVR",
                        "Los módulos visuales quedaron en Visuals/. Los colliders de interruptores, compuertas, pines, LED y Canvas se conservaron.",
                        "Entendido");
                }

                return true;
            }
            catch (System.Exception exception)
            {
                Undo.RevertAllDownToGroup(undo);
                Debug.LogError("GateVR: la configuración se revirtió.\n" + exception.Message);
                if (interactive)
                    EditorUtility.DisplayDialog("GateVR", "La escena se revirtió.\n\n" + exception.Message, "Entendido");
                return false;
            }
        }

        static bool HasSerializedProperties(SwitchView[] switches, LedView[] leds, OutputPinView[] outputs, StringBuilder errors)
        {
            bool ok = true;
            for (int i = 0; i < switches.Length; i++)
            {
                ok &= PropertyExists(switches[i], "meshRenderer", errors);
                ok &= PropertyExists(switches[i], "offMaterial", errors);
                ok &= PropertyExists(switches[i], "onMaterial", errors);
            }

            for (int i = 0; i < leds.Length; i++)
                ok &= PropertyExists(leds[i], "bulbRenderer", errors);

            for (int i = 0; i < outputs.Length; i++)
                ok &= PropertyExists(outputs[i], "highlightRenderer", errors);
            for (int i = 0; i < outputs.Length; i++)
                ok &= PropertyExists(outputs[i], "connectionAnchor", errors);

            InputPinView[] inputPins = UnityEngine.Object.FindObjectsByType<InputPinView>();
            for (int i = 0; i < inputPins.Length; i++)
                ok &= PropertyExists(inputPins[i], "connectionAnchor", errors);

            return ok;
        }

        static bool PropertyExists(Object component, string propertyName, StringBuilder errors)
        {
            SerializedObject serialized = new SerializedObject(component);
            if (serialized.FindProperty(propertyName) != null)
                return true;

            errors.AppendLine(component.GetType().Name + " no tiene la propiedad " + propertyName + ".");
            return false;
        }

        static bool BackupOriginalScene(StringBuilder errors)
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            string destination = Path.Combine(project, BackupPath);
            if (File.Exists(destination))
                return true;

            string source = Path.Combine(project, ScenePath);
            if (!File.Exists(source))
            {
                errors.AppendLine("No existe " + ScenePath + " para copiar la escena original.");
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(source, destination);
            return true;
        }

        static float ResolveSupport(Scene scene)
        {
            GameObject table = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "Mesa")
                {
                    table = roots[i];
                    break;
                }
            }

            if (table == null)
                return 0.8f;

            return table.transform.position.y + Mathf.Abs(table.transform.lossyScale.y) * 0.5f + 0.012f;
        }

        static void BuildBoard(Scene scene, float support)
        {
            Transform lab = null;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "PracticeLab")
                {
                    lab = roots[i].transform;
                    break;
                }
            }

            if (lab == null)
                return;

            Transform visuals = LabVisualLibrary.ReplaceVisuals(lab);
            float thickness = 0.024f;
            float scale = Mathf.Abs(lab.lossyScale.y);
            if (scale < 0.0001f)
                scale = 1f;
            float centerY = (support - lab.position.y) / scale - thickness * 0.5f;
            LabVisualLibrary.Part(
                visuals, PrimitiveType.Cube, "Placa",
                new Vector3(0f, centerY, 0f),
                new Vector3(1.55f, thickness, 0.95f),
                Quaternion.identity,
                LabVisualLibrary.Board);

            LabVisualLibrary.Part(
                visuals, PrimitiveType.Cube, "Trazo",
                new Vector3(0f, centerY + thickness * 0.55f, 0f),
                new Vector3(1.15f, 0.004f, 0.08f),
                Quaternion.identity,
                LabVisualLibrary.Copper);

            Quaternion flat = Quaternion.Euler(0f, 0f, 0f);
            for (int x = -5; x <= 5; x++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    LabVisualLibrary.Part(
                        visuals, PrimitiveType.Cylinder, "Orificio",
                        new Vector3(x * 0.11f, centerY + thickness * 0.45f, side * 0.36f),
                        new Vector3(0.035f, 0.004f, 0.035f),
                        flat,
                        LabVisualLibrary.HousingDark);
                }
            }

            Vector3[] feet =
            {
                new Vector3(-0.72f, centerY, -0.42f),
                new Vector3(0.72f, centerY, -0.42f),
                new Vector3(-0.72f, centerY, 0.42f),
                new Vector3(0.72f, centerY, 0.42f)
            };
            for (int i = 0; i < feet.Length; i++)
            {
                LabVisualLibrary.Part(
                    visuals, PrimitiveType.Cylinder, "Tornillo",
                    feet[i],
                    new Vector3(0.05f, 0.008f, 0.05f),
                    Quaternion.identity,
                    LabVisualLibrary.Screw);
            }
        }

        static void BuildSwitch(SwitchView view, float support, StringBuilder errors)
        {
            Transform host = view.transform;
            float bottom = LabVisualLibrary.LocalBottom(host, support);
            Transform visuals = LabVisualLibrary.ReplaceVisuals(host);
            const float baseHeight = 0.16f;
            const float bodyHeight = 0.32f;
            float baseY = bottom + baseHeight * 0.5f;
            float bodyY = bottom + baseHeight + bodyHeight * 0.5f;
            float rockerY = bottom + baseHeight + bodyHeight + 0.06f;

            LabVisualLibrary.Part(visuals, PrimitiveType.Cube, "Base", new Vector3(-0.02f, baseY, 0f), new Vector3(0.82f, baseHeight, 0.7f), Quaternion.identity, LabVisualLibrary.HousingDark);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cube, "Cuerpo", new Vector3(-0.02f, bodyY, 0f), new Vector3(0.56f, bodyHeight, 0.48f), Quaternion.identity, LabVisualLibrary.Housing);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cube, "Bisel", new Vector3(-0.02f, bodyY, -0.22f), new Vector3(0.5f, bodyHeight * 0.72f, 0.04f), Quaternion.identity, LabVisualLibrary.HousingDark);
            GameObject rocker = LabVisualLibrary.Part(
                visuals, PrimitiveType.Cube, "Balancin",
                new Vector3(-0.02f, rockerY, -0.02f),
                new Vector3(0.28f, 0.1f, 0.22f),
                Quaternion.identity,
                LabVisualLibrary.SwitchOff);
            LabVisualLibrary.Part(
                visuals, PrimitiveType.Cylinder, "Eje",
                new Vector3(-0.02f, rockerY - 0.04f, 0f),
                new Vector3(0.08f, 0.16f, 0.08f),
                Quaternion.Euler(0f, 0f, 90f),
                LabVisualLibrary.Metal);

            string mark = host.name.EndsWith("_A") ? "A" : "B";
            OutputPinView switchPin = host.GetComponentInChildren<OutputPinView>();
            if (switchPin != null)
                PlacePin(switchPin.transform, new Vector3(0.74f, bodyY, 0f), 0.52f);

            LabVisualLibrary.Label(
                visuals, mark,
                new Vector3(-0.02f, bodyY, -0.3f),
                0.36f,
                new Color(0.93f, 0.95f, 0.9f));

            if (switchPin != null)
            {
                Vector3 pin = switchPin.transform.localPosition;
                Lead(visuals, new Vector3(0.28f, pin.y, pin.z), pin + new Vector3(-0.08f, 0f, 0f), 0.045f);
            }
            LabVisualLibrary.HideRootMesh(host.gameObject);
            LabVisualLibrary.FitBox(host.gameObject, new Vector3(-0.02f, bottom + 0.32f, 0f), new Vector3(0.84f, 0.64f, 0.72f));
            MeshRenderer rockerRenderer = rocker.GetComponent<MeshRenderer>();
            if (!LabVisualLibrary.AssignReference(view, "meshRenderer", rockerRenderer, errors))
                return;
            if (!LabVisualLibrary.AssignIfMissing(view, "offMaterial", LabVisualLibrary.SwitchOff, errors))
                return;
            LabVisualLibrary.AssignIfMissing(view, "onMaterial", LabVisualLibrary.SwitchOn, errors);
        }

        static void BuildGate(GateView view, float support, StringBuilder errors)
        {
            Transform host = view.transform;
            float bottom = LabVisualLibrary.LocalBottom(host, support);
            Transform visuals = LabVisualLibrary.ReplaceVisuals(host);
            const float height = 0.28f;
            const float bodyDepth = 0.58f;
            float bodyY = bottom + height * 0.5f;
            PlaceGatePins(host, bodyY);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cube, "Capsula", new Vector3(0f, bodyY, 0f), new Vector3(0.98f, height, bodyDepth), Quaternion.identity, LabVisualLibrary.Housing);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cube, "Tapa", new Vector3(0f, bodyY + height * 0.28f, 0f), new Vector3(0.72f, height * 0.28f, 0.28f), Quaternion.identity, LabVisualLibrary.HousingDark);
            LabVisualLibrary.Part(visuals, PrimitiveType.Sphere, "Muesca", new Vector3(-0.42f, bodyY + height * 0.35f, 0f), Vector3.one * 0.08f, Quaternion.identity, LabVisualLibrary.HousingDark);

            float labelY = bodyY + height * 0.62f;
            LabVisualLibrary.Label(
                visuals, view.Type.ToString(),
                new Vector3(0f, labelY, 0f),
                0.42f,
                new Color(0.95f, 0.93f, 0.78f));

            InputPinView[] ports = host.GetComponentsInChildren<InputPinView>();
            for (int i = 0; i < ports.Length; i++)
            {
                if (ports[i].IsLedInput)
                    continue;

                Vector3 port = ports[i].transform.localPosition;
                string portName = ports[i].IsPortB ? "B" : "A";
                LabVisualLibrary.Label(
                    visuals, portName,
                    new Vector3(-0.18f, labelY, port.z),
                    0.24f,
                    Color.white);
                Lead(visuals, new Vector3(-0.49f, port.y, port.z), port + new Vector3(0.06f, 0f, 0f), 0.04f);
            }

            OutputPinView[] gateOutputs = host.GetComponentsInChildren<OutputPinView>();
            for (int i = 0; i < gateOutputs.Length; i++)
            {
                Vector3 port = gateOutputs[i].transform.localPosition;
                LabVisualLibrary.Label(
                    visuals, "OUTPUT",
                    new Vector3(0.2f, labelY, 0f),
                    0.4f,
                    new Color(1f, 0.86f, 0.45f));
                Lead(visuals, new Vector3(0.49f, port.y, port.z), port + new Vector3(-0.06f, 0f, 0f), 0.04f);
            }

            LabVisualLibrary.HideRootMesh(host.gameObject);
            LabVisualLibrary.FitBox(host.gameObject, new Vector3(0f, bodyY, 0f), new Vector3(0.98f, height, bodyDepth));
        }

        static void BuildLed(LedView view, float support, StringBuilder errors)
        {
            Transform host = view.transform;
            float bottom = LabVisualLibrary.LocalBottom(host, support);
            Transform visuals = LabVisualLibrary.ReplaceVisuals(host);
            const float socketHeight = 0.62f;
            float socketY = bottom + socketHeight * 0.5f;
            Vector3 socket = new Vector3(0.18f, socketY, 0f);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cube, "Zocalo", socket, new Vector3(0.62f, socketHeight, 0.62f), Quaternion.identity, LabVisualLibrary.HousingDark);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cylinder, "Cuello", new Vector3(0.18f, bottom + socketHeight + 0.08f, 0f), new Vector3(0.28f, 0.08f, 0.28f), Quaternion.identity, LabVisualLibrary.Metal);
            GameObject bulb = LabVisualLibrary.Part(
                visuals, PrimitiveType.Sphere, "Bombilla",
                new Vector3(0.18f, bottom + socketHeight + 0.28f, 0f),
                Vector3.one * 0.5f,
                Quaternion.identity,
                LabVisualLibrary.LedOff);
            LabVisualLibrary.Part(
                visuals, PrimitiveType.Sphere, "Brillo",
                new Vector3(0.28f, bottom + socketHeight + 0.38f, -0.08f),
                Vector3.one * 0.12f,
                Quaternion.identity,
                LabVisualLibrary.Glass);
            LabVisualLibrary.Label(
                visuals, "INPUT",
                new Vector3(0.18f, socketY, -0.4f),
                0.42f,
                new Color(0.8f, 0.92f, 1f));

            InputPinView ledPin = host.GetComponentInChildren<InputPinView>();
            if (ledPin != null)
            {
                PlacePin(ledPin.transform, new Vector3(-0.42f, socketY, 0f), 0.9f);
                Lead(visuals, new Vector3(-0.13f, socketY, 0f), new Vector3(-0.34f, socketY, 0f), 0.07f);
            }
            LabVisualLibrary.HideRootMesh(host.gameObject);
            LabVisualLibrary.FitBox(host.gameObject, new Vector3(0.18f, bottom + 0.48f, 0f), new Vector3(0.66f, 0.96f, 0.66f));
            MeshRenderer bulbRenderer = bulb.GetComponent<MeshRenderer>();
            LabVisualLibrary.AssignReference(view, "bulbRenderer", bulbRenderer, errors);
        }

        static void BuildOutputPin(OutputPinView view, StringBuilder errors)
        {
            MeshRenderer cap = BuildTerminal(view.transform, LabVisualLibrary.PinOutput);
            LabVisualLibrary.AssignReference(view, "highlightRenderer", cap, errors);
            LabVisualLibrary.AssignReference(view, "connectionAnchor", cap.transform, errors);
        }

        static void BuildInputPin(InputPinView view, StringBuilder errors)
        {
            if (view == null)
            {
                errors.AppendLine("Hay un pin de entrada sin componente.");
                return;
            }

            MeshRenderer cap = BuildTerminal(view.transform, LabVisualLibrary.PinInput);
            LabVisualLibrary.AssignReference(view, "connectionAnchor", cap.transform, errors);
        }

        static MeshRenderer BuildTerminal(Transform pin, Material capMaterial)
        {
            Transform visuals = LabVisualLibrary.ReplaceVisuals(pin);
            Vector3 axis = pin.localPosition.sqrMagnitude > 0.0001f ? pin.localPosition.normalized : Vector3.right;
            Quaternion align = Quaternion.FromToRotation(Vector3.up, axis);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cylinder, "Canion", axis * 0.02f, new Vector3(0.38f, 0.2f, 0.38f), align, LabVisualLibrary.Metal);
            LabVisualLibrary.Part(visuals, PrimitiveType.Cylinder, "Aislante", axis * 0.24f, new Vector3(0.52f, 0.045f, 0.52f), align, LabVisualLibrary.HousingDark);
            GameObject cap = LabVisualLibrary.Part(visuals, PrimitiveType.Sphere, "Cabeza", axis * 0.42f, Vector3.one * 0.48f, Quaternion.identity, capMaterial);
            LabVisualLibrary.HideRootMesh(pin.gameObject);
            LabVisualLibrary.FitBox(pin.gameObject, axis * 0.28f, new Vector3(0.62f, 0.62f, 0.62f));
            return cap.GetComponent<MeshRenderer>();
        }

        static void PlaceCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
            Canvas canvas = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].name == "Canvas")
                {
                    canvas = canvases[i];
                    break;
                }
            }

            if (canvas == null)
                return;

            RectTransform rect = canvas.GetComponent<RectTransform>();
            Undo.RecordObject(canvas, "Colocar panel");
            Undo.RecordObject(rect, "Colocar panel");
            canvas.renderMode = RenderMode.WorldSpace;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(920f, 560f);
            rect.localScale = new Vector3(0.00115f, 0.00115f, 0.00115f);
            rect.position = new Vector3(0.78f, 1.18f, 0.08f);

            Camera camera = Camera.main;
            Vector3 towardUser = camera != null
                ? camera.transform.position - rect.position
                : new Vector3(-0.15f, 0.7f, -1f);
            if (towardUser.sqrMagnitude > 0.0001f)
                rect.rotation = Quaternion.LookRotation(-towardUser, Vector3.up);
        }

        static void PlaceGatePins(Transform gate, float bodyY)
        {
            InputPinView[] inputs = gate.GetComponentsInChildren<InputPinView>();
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].IsLedInput)
                    continue;

                float side = inputs[i].IsPortB ? -0.22f : 0.22f;
                PlacePin(inputs[i].transform, new Vector3(-0.66f, bodyY, side), 0.58f);
            }

            OutputPinView[] outputs = gate.GetComponentsInChildren<OutputPinView>();
            for (int i = 0; i < outputs.Length; i++)
                PlacePin(outputs[i].transform, new Vector3(0.66f, bodyY, 0f), 0.58f);
        }

        static void PlacePin(Transform pin, Vector3 localPosition, float uniformScale)
        {
            Undo.RecordObject(pin, "Colocar pin");
            pin.localPosition = localPosition;
            pin.localRotation = Quaternion.identity;
            pin.localScale = Vector3.one * uniformScale;
        }

        static void Lead(Transform visuals, Vector3 from, Vector3 to, float thickness)
        {
            Vector3 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.001f)
                return;

            LabVisualLibrary.Part(
                visuals,
                PrimitiveType.Cylinder,
                "Terminal",
                (from + to) * 0.5f,
                new Vector3(thickness, length * 0.5f, thickness),
                Quaternion.FromToRotation(Vector3.up, delta),
                LabVisualLibrary.Metal);
        }

        static bool Audit(
            SwitchView[] switches,
            GateView[] gates,
            LedView[] leds,
            OutputPinView[] outputs,
            InputPinView[] inputs,
            VrPointerBinding binding,
            StringBuilder errors)
        {
            if (binding == null)
                errors.AppendLine("VrPointerBinding desapareció durante la configuración.");

            AuditHosts(switches, errors);
            AuditHosts(gates, errors);
            AuditHosts(leds, errors);
            AuditHosts(outputs, errors);
            AuditHosts(inputs, errors);
            CountDecorativeColliders(errors);
            return errors.Length == 0;
        }

        static void AuditHosts(Component[] hosts, StringBuilder errors)
        {
            for (int i = 0; i < hosts.Length; i++)
            {
                if (hosts[i].GetComponent<Collider>() == null)
                    errors.AppendLine(hosts[i].name + " perdió su collider funcional.");

                Transform visuals = hosts[i].transform.Find(LabVisualLibrary.VisualsName);
                if (visuals == null)
                    errors.AppendLine(hosts[i].name + " no tiene Visuals.");
            }
        }

        static void CountDecorativeColliders(StringBuilder errors)
        {
            Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name != LabVisualLibrary.VisualsName)
                    continue;

                Collider[] colliders = all[i].GetComponentsInChildren<Collider>(true);
                if (colliders.Length > 0)
                    errors.AppendLine(all[i].parent.name + "/Visuals todavía tiene " + colliders.Length + " colliders.");
            }
        }

        static bool Fail(bool interactive, string message)
        {
            Debug.LogError("GateVR: configuración cancelada.\n" + message);
            if (interactive)
                EditorUtility.DisplayDialog("GateVR", message, "Entendido");
            return false;
        }
    }
}
