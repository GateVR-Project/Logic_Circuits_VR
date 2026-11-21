using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GateVR.EditorTools
{
    /// <summary>
    /// Materiales y piezas que pertenecen solo al configurador visual.
    /// No reescribe LedOn, LedOff, SwitchOn ni SwitchOff.
    /// </summary>
    static class LabVisualLibrary
    {
        public const string VisualsName = "Visuals";
        public const string LitShaderGuid = "933532a4fcc9baf4fa0491de14d08ed7";
        public const string Folder = "Assets/_GateVR/Presentation/Materials/LabVisuals";
        public const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        public const string SwitchOffPath = "Assets/_GateVR/Presentation/Materials/SwitchOff.mat";
        public const string SwitchOnPath = "Assets/_GateVR/Presentation/Materials/SwitchOn.mat";
        public const string LedOffPath = "Assets/_GateVR/Presentation/Materials/LedOff.mat";
        public const string LedOnPath = "Assets/_GateVR/Presentation/Materials/LedOn.mat";

        public static Shader LitShader { get; private set; }
        public static TMP_FontAsset Font { get; private set; }

        public static Material Housing { get; private set; }
        public static Material HousingDark { get; private set; }
        public static Material Metal { get; private set; }
        public static Material PinOutput { get; private set; }
        public static Material PinInput { get; private set; }
        public static Material Board { get; private set; }
        public static Material Copper { get; private set; }
        public static Material Screw { get; private set; }
        public static Material Glass { get; private set; }
        public static Material SwitchOff { get; private set; }
        public static Material SwitchOn { get; private set; }
        public static Material LedOff { get; private set; }
        public static Material LedOn { get; private set; }

        public static bool TryPrepare(StringBuilder errors)
        {
            LitShader = LoadLitShader();
            if (LitShader == null)
            {
                errors.AppendLine("No está el shader Universal Render Pipeline/Lit (guid " + LitShaderGuid + "). No se crean materiales.");
                return false;
            }

            if (LitShader.FindPropertyIndex("_BaseColor") < 0 || LitShader.FindPropertyIndex("_Smoothness") < 0)
            {
                errors.AppendLine("El shader Lit no expone _BaseColor y _Smoothness. No se asignan propiedades inexistentes.");
                return false;
            }

            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (Font == null)
            {
                errors.AppendLine("No está la fuente " + FontPath + ". Las etiquetas no se pueden generar.");
                return false;
            }

            SwitchOff = LoadOwnedMaterial(SwitchOffPath, errors);
            SwitchOn = LoadOwnedMaterial(SwitchOnPath, errors);
            LedOff = LoadOwnedMaterial(LedOffPath, errors);
            LedOn = LoadOwnedMaterial(LedOnPath, errors);
            if (SwitchOff == null || SwitchOn == null || LedOff == null || LedOn == null)
                return false;

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_GateVR/Presentation/Materials"))
                {
                    errors.AppendLine("No existe Assets/_GateVR/Presentation/Materials.");
                    return false;
                }

                AssetDatabase.CreateFolder("Assets/_GateVR/Presentation/Materials", "LabVisuals");
            }

            Housing = EnsureMaterial("Housing.mat", new Color(0.17f, 0.22f, 0.30f), 0.08f, 0.42f);
            HousingDark = EnsureMaterial("HousingDark.mat", new Color(0.07f, 0.08f, 0.10f), 0.15f, 0.35f);
            Metal = EnsureMaterial("Metal.mat", new Color(0.72f, 0.74f, 0.78f), 0.9f, 0.68f);
            PinOutput = EnsureMaterial("PinOutput.mat", new Color(0.86f, 0.58f, 0.18f), 0.82f, 0.62f);
            PinInput = EnsureMaterial("PinInput.mat", new Color(0.78f, 0.82f, 0.88f), 0.88f, 0.7f);
            Board = EnsureMaterial("Board.mat", new Color(0.04f, 0.27f, 0.16f), 0.02f, 0.22f);
            Copper = EnsureMaterial("Copper.mat", new Color(0.70f, 0.36f, 0.16f), 0.55f, 0.48f);
            Screw = EnsureMaterial("Screw.mat", new Color(0.45f, 0.47f, 0.50f), 0.95f, 0.55f);
            Glass = EnsureMaterial("Glass.mat", new Color(0.82f, 0.93f, 0.86f), 0.0f, 0.88f);
            return Housing != null && PinOutput != null && Board != null;
        }

        static Shader LoadLitShader()
        {
            string path = AssetDatabase.GUIDToAssetPath(LitShaderGuid);
            if (string.IsNullOrEmpty(path))
                return null;

            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }

        static Material LoadOwnedMaterial(string path, StringBuilder errors)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                errors.AppendLine("Falta el material obligatorio " + path + ".");
                return null;
            }

            if (material.shader == null)
            {
                errors.AppendLine("El material " + path + " no tiene shader.");
                return null;
            }

            return material;
        }

        static Material EnsureMaterial(string fileName, Color baseColor, float metallic, float smoothness)
        {
            string path = Folder + "/" + fileName;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(LitShader);
            material.name = Path.GetFileNameWithoutExtension(fileName);
            if (LitShader.FindPropertyIndex("_BaseColor") >= 0)
                material.SetColor("_BaseColor", baseColor);
            if (LitShader.FindPropertyIndex("_Metallic") >= 0)
                material.SetFloat("_Metallic", metallic);
            if (LitShader.FindPropertyIndex("_Smoothness") >= 0)
                material.SetFloat("_Smoothness", smoothness);

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        public static Transform ReplaceVisuals(Transform host)
        {
            Transform previous = host.Find(VisualsName);
            if (previous != null)
                Undo.DestroyObjectImmediate(previous.gameObject);

            GameObject root = new GameObject(VisualsName);
            Undo.RegisterCreatedObjectUndo(root, "Visuals");
            root.transform.SetParent(host, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root.transform;
        }

        public static GameObject Part(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Las piezas decorativas solo se construyen en el editor, fuera de Play Mode.");

            GameObject piece = GameObject.CreatePrimitive(type);
            piece.name = name;
            Undo.RegisterCreatedObjectUndo(piece, "Pieza visual");
            RemoveGeneratedCollider(piece);
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localRotation = localRotation;
            piece.transform.localScale = localScale;

            MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            return piece;
        }

        public static void RemoveGeneratedCollider(GameObject decorative)
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("DestroyImmediate de colliders decorativos no se ejecuta en Play Mode.");

            Collider generated = decorative.GetComponent<Collider>();
            if (generated != null)
                UnityEngine.Object.DestroyImmediate(generated);
        }

        public static void Label(Transform parent, string text, Vector3 localPosition, Quaternion localRotation, float width, Color color)
        {
            GameObject plate = new GameObject("Label_" + text);
            Undo.RegisterCreatedObjectUndo(plate, "Etiqueta");
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = localPosition;
            plate.transform.localRotation = localRotation;
            plate.transform.localScale = Vector3.one;

            TextMeshPro tmp = plate.AddComponent<TextMeshPro>();
            tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = 3.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.rectTransform.sizeDelta = new Vector2(width, width * 0.42f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.ForceMeshUpdate();
        }

        public static void HideRootMesh(GameObject host)
        {
            MeshRenderer renderer = host.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            Undo.RecordObject(renderer, "Ocultar primitiva original");
            renderer.enabled = false;
        }

        public static void FitBox(GameObject host, Vector3 center, Vector3 size)
        {
            BoxCollider box = host.GetComponent<BoxCollider>();
            if (box == null)
                return;

            Undo.RecordObject(box, "Alinear collider con el modelo");
            box.center = center;
            box.size = size;
        }

        public static float LocalBottom(Transform host, float supportWorldY)
        {
            float scale = Mathf.Abs(host.lossyScale.y);
            if (scale < 0.0001f)
                scale = 1f;

            return (supportWorldY - host.position.y) / scale;
        }

        public static bool AssignReference(UnityEngine.Object component, string propertyName, UnityEngine.Object value, StringBuilder errors)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                errors.AppendLine(component.GetType().Name + " no tiene la propiedad serializada " + propertyName + ".");
                return false;
            }

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                errors.AppendLine(propertyName + " no es una referencia de objeto.");
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            return true;
        }

        public static bool AssignIfMissing(UnityEngine.Object component, string propertyName, UnityEngine.Object value, StringBuilder errors)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                errors.AppendLine(component.GetType().Name + " no tiene la propiedad serializada " + propertyName + ".");
                return false;
            }

            if (property.objectReferenceValue == null)
                property.objectReferenceValue = value;

            serialized.ApplyModifiedProperties();
            return true;
        }
    }
}
