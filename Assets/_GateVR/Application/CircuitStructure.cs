using System;
using System.Collections.Generic;
using GateVR.Domain.Entities;

namespace GateVR.Application
{
    /// <summary>
    /// Compuerta presente en la placa. El id lo asigna la estación, no el nodo lógico.
    /// </summary>
    public sealed class StationGate
    {
        public StationGate(string id, GateNode node)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El módulo necesita un id.", nameof(id));

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Id = id;
            Node = node;
        }

        public string Id { get; private set; }

        public GateNode Node { get; private set; }
    }

    /// <summary>
    /// Módulo que una práctica compuesta exige ver en la placa.
    /// </summary>
    public sealed class CircuitModuleSpec
    {
        public CircuitModuleSpec(string id, string gateType)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El módulo necesita un id.", nameof(id));

            if (string.IsNullOrWhiteSpace(gateType))
                throw new ArgumentException("El módulo necesita un tipo de compuerta.", nameof(gateType));

            Id = id;
            GateType = gateType;
        }

        public string Id { get; private set; }

        public string GateType { get; private set; }
    }

    /// <summary>
    /// Cable exigido. El origen puede ser "A", "B" o el id de un módulo.
    /// El destino es el id de un módulo o "LED".
    /// </summary>
    public sealed class CircuitLinkSpec
    {
        public const string SwitchA = "A";
        public const string SwitchB = "B";
        public const string Led = "LED";

        public CircuitLinkSpec(string fromId, string toId, string port)
        {
            if (string.IsNullOrWhiteSpace(fromId))
                throw new ArgumentException("El cable necesita un origen.", nameof(fromId));

            if (string.IsNullOrWhiteSpace(toId))
                throw new ArgumentException("El cable necesita un destino.", nameof(toId));

            FromId = fromId;
            ToId = toId;
            Port = string.IsNullOrWhiteSpace(port) ? "A" : port;
        }

        public string FromId { get; private set; }

        public string ToId { get; private set; }

        public string Port { get; private set; }
    }

    /// <summary>
    /// Topología que la práctica exige, además de la tabla de verdad.
    /// Una NAND integrada no cumple una práctica que pide AND seguida de NOT.
    /// </summary>
    public sealed class CircuitStructure
    {
        public CircuitStructure(IReadOnlyList<CircuitModuleSpec> modules, IReadOnlyList<CircuitLinkSpec> links)
        {
            if (modules == null || modules.Count < 2)
                throw new ArgumentException("Un circuito compuesto necesita al menos dos módulos.", nameof(modules));

            if (links == null || links.Count < 1)
                throw new ArgumentException("Un circuito compuesto necesita sus cables.", nameof(links));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var copies = new List<CircuitModuleSpec>(modules.Count);
            for (int i = 0; i < modules.Count; i++)
            {
                CircuitModuleSpec module = modules[i];
                if (module == null)
                    throw new ArgumentException("La lista de módulos tiene un elemento vacío.", nameof(modules));

                if (!ids.Add(module.Id))
                    throw new ArgumentException("El módulo '" + module.Id + "' está repetido.", nameof(modules));

                copies.Add(module);
            }

            bool reachesLed = false;
            var linkCopies = new List<CircuitLinkSpec>(links.Count);
            for (int i = 0; i < links.Count; i++)
            {
                CircuitLinkSpec link = links[i];
                if (link == null)
                    throw new ArgumentException("La lista de cables tiene un elemento vacío.", nameof(links));

                if (!IsSource(link.FromId, ids))
                    throw new ArgumentException("El cable sale de '" + link.FromId + "', que no existe.", nameof(links));

                if (string.Equals(link.ToId, CircuitLinkSpec.Led, StringComparison.OrdinalIgnoreCase))
                    reachesLed = true;
                else if (!ids.Contains(link.ToId))
                    throw new ArgumentException("El cable entra a '" + link.ToId + "', que no existe.", nameof(links));

                linkCopies.Add(link);
            }

            if (!reachesLed)
                throw new ArgumentException("El circuito compuesto tiene que llegar al LED.", nameof(links));

            Modules = copies.AsReadOnly();
            Links = linkCopies.AsReadOnly();
        }

        public IReadOnlyList<CircuitModuleSpec> Modules { get; private set; }

        public IReadOnlyList<CircuitLinkSpec> Links { get; private set; }

        public static CircuitStructure NandFromGates()
        {
            return new CircuitStructure(
                new[]
                {
                    new CircuitModuleSpec("m1", "AND"),
                    new CircuitModuleSpec("m2", "NOT")
                },
                new[]
                {
                    new CircuitLinkSpec(CircuitLinkSpec.SwitchA, "m1", "A"),
                    new CircuitLinkSpec(CircuitLinkSpec.SwitchB, "m1", "B"),
                    new CircuitLinkSpec("m1", "m2", "A"),
                    new CircuitLinkSpec("m2", CircuitLinkSpec.Led, "Y")
                });
        }

        public static CircuitStructure NorFromGates()
        {
            return new CircuitStructure(
                new[]
                {
                    new CircuitModuleSpec("m1", "OR"),
                    new CircuitModuleSpec("m2", "NOT")
                },
                new[]
                {
                    new CircuitLinkSpec(CircuitLinkSpec.SwitchA, "m1", "A"),
                    new CircuitLinkSpec(CircuitLinkSpec.SwitchB, "m1", "B"),
                    new CircuitLinkSpec("m1", "m2", "A"),
                    new CircuitLinkSpec("m2", CircuitLinkSpec.Led, "Y")
                });
        }

        /// <summary>
        /// Y = (A AND B) OR (NOT A). El interruptor A alimenta dos compuertas.
        /// </summary>
        public static CircuitStructure AndOrNotBranch()
        {
            return new CircuitStructure(
                new[]
                {
                    new CircuitModuleSpec("m1", "AND"),
                    new CircuitModuleSpec("m2", "NOT"),
                    new CircuitModuleSpec("m3", "OR")
                },
                new[]
                {
                    new CircuitLinkSpec(CircuitLinkSpec.SwitchA, "m1", "A"),
                    new CircuitLinkSpec(CircuitLinkSpec.SwitchB, "m1", "B"),
                    new CircuitLinkSpec(CircuitLinkSpec.SwitchA, "m2", "A"),
                    new CircuitLinkSpec("m1", "m3", "A"),
                    new CircuitLinkSpec("m2", "m3", "B"),
                    new CircuitLinkSpec("m3", CircuitLinkSpec.Led, "Y")
                });
        }

        private static bool IsSource(string id, HashSet<string> modules)
        {
            if (string.Equals(id, CircuitLinkSpec.SwitchA, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(id, CircuitLinkSpec.SwitchB, StringComparison.OrdinalIgnoreCase))
                return true;

            return modules.Contains(id);
        }
    }
}
