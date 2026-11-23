using System;
using System.Collections.Generic;

namespace GateVR.Application
{
    /// <summary>
    /// Definición de un ejercicio. <see cref="ExpectedTruthTable"/> es una función pura de A y B.
    /// </summary>
    public class PracticeExercise
    {
        public PracticeExercise(
            string id,
            string title,
            string objective,
            string logicConditionText,
            string requiredGateType,
            Func<bool, bool, bool> expectedTruthTable,
            bool usesSingleInput = false,
            string initialGateType = null,
            bool keepCompatibleWiring = true,
            string moduleName = null,
            IReadOnlyList<PracticeTaskDefinition> tasks = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("El ejercicio necesita un id.", nameof(id));

            if (string.IsNullOrWhiteSpace(requiredGateType))
                throw new ArgumentException("El ejercicio necesita el tipo de compuerta.", nameof(requiredGateType));

            if (expectedTruthTable == null)
                throw new ArgumentNullException(nameof(expectedTruthTable));

            Id = id;
            Title = title ?? string.Empty;
            Objective = objective ?? string.Empty;
            LogicConditionText = logicConditionText ?? string.Empty;
            RequiredGateType = requiredGateType;
            ExpectedTruthTable = expectedTruthTable;
            UsesSingleInput = usesSingleInput;
            InitialGateType = string.IsNullOrWhiteSpace(initialGateType)
                ? requiredGateType
                : initialGateType;
            KeepCompatibleWiring = keepCompatibleWiring;
            ModuleName = string.IsNullOrWhiteSpace(moduleName) ? "Fundamentos" : moduleName;
            PracticeTaskDefinition.ValidateSet(tasks);
            Tasks = new List<PracticeTaskDefinition>(tasks).AsReadOnly();
        }

        public string Id { get; private set; }

        public string Title { get; private set; }

        public string Objective { get; private set; }

        public string LogicConditionText { get; private set; }

        public string RequiredGateType { get; private set; }

        public Func<bool, bool, bool> ExpectedTruthTable { get; private set; }

        /// <summary>
        /// NOT evalúa solo A. AND y OR usan las dos entradas.
        /// </summary>
        public bool UsesSingleInput { get; private set; }

        /// <summary>
        /// Compuerta que la estación muestra al entrar en la práctica.
        /// En una lección introductoria coincide con <see cref="RequiredGateType"/>.
        /// Una práctica de diagnóstico puede empezar con otra.
        /// </summary>
        public string InitialGateType { get; private set; }

        /// <summary>
        /// Al entrar, se conservan los cables que el tipo nuevo admite.
        /// Si es falso, la práctica empieza sin cables.
        /// </summary>
        public bool KeepCompatibleWiring { get; private set; }

        /// <summary>
        /// Submódulo pedagógico. Las tres lecciones actuales pertenecen a Fundamentos.
        /// </summary>
        public string ModuleName { get; private set; }

        public IReadOnlyList<PracticeTaskDefinition> Tasks { get; private set; }
    }
}
