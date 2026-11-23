using System;

namespace GateVR.Application
{
    /// <summary>
    /// Cableado que la estación coloca al entrar o al reiniciar.
    /// </summary>
    public sealed class StationPreset
    {
        public StationPreset(
            bool wireSwitches,
            bool wireGates,
            bool wireLed,
            string omitModuleId = null,
            string omitPort = null)
        {
            WireSwitches = wireSwitches;
            WireGates = wireGates;
            WireLed = wireLed;
            OmitModuleId = omitModuleId ?? string.Empty;
            OmitPort = string.IsNullOrWhiteSpace(omitPort) ? string.Empty : omitPort;
        }

        public bool WireSwitches { get; private set; }

        public bool WireGates { get; private set; }

        public bool WireLed { get; private set; }

        public string OmitModuleId { get; private set; }

        public string OmitPort { get; private set; }

        public bool Omits(string moduleId, string port)
        {
            return !string.IsNullOrEmpty(OmitModuleId)
                && string.Equals(OmitModuleId, moduleId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(OmitPort, port, StringComparison.OrdinalIgnoreCase);
        }
    }
}
