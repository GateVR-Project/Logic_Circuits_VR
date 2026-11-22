using UnityEngine;

namespace GateVR.Presentation
{
    /// <summary>
    /// Punto donde debe terminar el cable: la cabeza del terminal, no el centro del módulo.
    /// </summary>
    public static class PinConnection
    {
        public static Vector3 Resolve(Transform anchor, Component pin)
        {
            if (anchor != null)
                return anchor.position;

            Collider collider = pin.GetComponent<Collider>();
            if (collider != null)
                return collider.bounds.center;

            return pin.transform.position;
        }
    }
}
