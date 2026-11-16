using System;

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
            Func<bool, bool, bool> expectedTruthTable)
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
        }

        public string Id { get; private set; }

        public string Title { get; private set; }

        public string Objective { get; private set; }

        public string LogicConditionText { get; private set; }

        public string RequiredGateType { get; private set; }

        public Func<bool, bool, bool> ExpectedTruthTable { get; private set; }
    }
}
