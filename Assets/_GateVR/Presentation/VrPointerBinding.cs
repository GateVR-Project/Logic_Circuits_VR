using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GateVR.Presentation
{
    /// <summary>
    /// Enlaza los colliders que ya existen con el RayInteractor del rig.
    /// No crea una segunda vía de interacción si el objeto ya tiene RayInteractable.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class VrPointerBinding : MonoBehaviour
    {
        private void Awake()
        {
            int switches = BindAll(FindObjectsByType<SwitchView>(), BindSwitch);
            int outputs = BindAll(FindObjectsByType<OutputPinView>(), BindOutput);
            int inputs = BindAll(FindObjectsByType<InputPinView>(), BindInput);
            bool panel = BindPanel();
            Debug.Log(
                "VrPointerBinding: " + switches + " interruptores, " +
                outputs + " pines de salida, " + inputs + " pines de entrada. Panel=" + panel + ".",
                this);
        }

        private static int BindAll<T>(T[] views, System.Action<T> bind) where T : Component
        {
            for (int i = 0; i < views.Length; i++)
                bind(views[i]);

            return views.Length;
        }

        private static void BindSwitch(SwitchView view)
        {
            RayInteractable ray = EnsureRay(view.gameObject);
            if (ray == null)
                return;

            InteractableUnityEventWrapper wrapper = EnsureWrapper(view.gameObject, ray);
            Listen(wrapper, view.Interact);
        }

        private static void BindOutput(OutputPinView view)
        {
            RayInteractable ray = EnsureRay(view.gameObject);
            if (ray == null)
                return;

            InteractableUnityEventWrapper wrapper = EnsureWrapper(view.gameObject, ray);
            Listen(wrapper, view.SelectPin);
        }

        private static void BindInput(InputPinView view)
        {
            RayInteractable ray = EnsureRay(view.gameObject);
            if (ray == null)
                return;

            InteractableUnityEventWrapper wrapper = EnsureWrapper(view.gameObject, ray);
            Listen(wrapper, view.SelectPin);
        }

        private static void Listen(InteractableUnityEventWrapper wrapper, UnityAction action)
        {
            if (wrapper.WhenSelect == null)
            {
                FieldInfo field = typeof(InteractableUnityEventWrapper).GetField(
                    "_whenSelect",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                field.SetValue(wrapper, new UnityEvent());
            }

            wrapper.WhenSelect.AddListener(action);
        }

        private static RayInteractable EnsureRay(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogWarning("VrPointerBinding: " + target.name + " no tiene Collider.", target);
                return null;
            }

            RayInteractable ray = target.GetComponent<RayInteractable>();
            if (ray != null)
                return ray;

            ColliderSurface surface = target.GetComponent<ColliderSurface>();
            if (surface == null)
                surface = target.AddComponent<ColliderSurface>();

            surface.InjectCollider(collider);
            ray = target.AddComponent<RayInteractable>();
            ray.InjectSurface(surface);
            return ray;
        }

        private static InteractableUnityEventWrapper EnsureWrapper(GameObject target, RayInteractable ray)
        {
            InteractableUnityEventWrapper wrapper = target.GetComponent<InteractableUnityEventWrapper>();
            if (wrapper == null)
                wrapper = target.AddComponent<InteractableUnityEventWrapper>();

            wrapper.InjectInteractableView(ray);
            return wrapper;
        }

        private static bool BindPanel()
        {
            PointableCanvas pointable = FindAnyObjectByType<PointableCanvas>(FindObjectsInactive.Include);
            if (pointable == null)
            {
                Debug.LogWarning("VrPointerBinding: el Canvas no tiene PointableCanvas.");
                return false;
            }

            Canvas canvas = pointable.GetComponent<Canvas>();
            RectTransform rect = pointable.GetComponent<RectTransform>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace || rect == null)
            {
                Debug.LogWarning("VrPointerBinding: el Canvas tiene que estar en World Space.", pointable);
                return false;
            }

            BoxCollider box = pointable.GetComponent<BoxCollider>();
            if (box == null)
                box = pointable.gameObject.AddComponent<BoxCollider>();

            box.center = Vector3.zero;
            box.size = new Vector3(Mathf.Max(rect.sizeDelta.x, 1f), Mathf.Max(rect.sizeDelta.y, 1f), 20f);

            ColliderSurface surface = pointable.GetComponent<ColliderSurface>();
            if (surface == null)
                surface = pointable.gameObject.AddComponent<ColliderSurface>();

            surface.InjectCollider(box);

            RayInteractable ray = pointable.GetComponent<RayInteractable>();
            if (ray == null)
                ray = pointable.gameObject.AddComponent<RayInteractable>();

            ray.InjectSurface(surface);
            ray.InjectOptionalPointableElement(pointable);
            pointable.enabled = true;
            ray.enabled = true;

            PointableCanvasModule module = FindAnyObjectByType<PointableCanvasModule>(FindObjectsInactive.Include);
            if (module == null)
            {
                EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
                if (eventSystem == null)
                {
                    Debug.LogWarning("VrPointerBinding: no hay EventSystem.");
                    return false;
                }

                module = eventSystem.gameObject.AddComponent<PointableCanvasModule>();
            }

            module.ExclusiveMode = true;
            module.enabled = true;
            return true;
        }
    }
}
