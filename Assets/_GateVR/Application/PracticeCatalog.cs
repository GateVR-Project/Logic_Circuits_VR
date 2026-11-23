using System.Collections.Generic;

namespace GateVR.Application
{
    /// <summary>
    /// Catálogo de Fundamentos y los primeros ejercicios de Aplicación Real.
    /// Diagnóstico queda para una iteración posterior.
    /// </summary>
    public static class PracticeCatalog
    {
        public static IReadOnlyList<PracticeExercise> Create()
        {
            return new List<PracticeExercise>
            {
                Binary(
                    "and-01",
                    "Compuerta AND",
                    "Construye un circuito donde el LED se encienda únicamente cuando A y B estén encendidos.",
                    "Y = A AND B",
                    "AND",
                    (a, b) => a && b),
                Binary(
                    "or-01",
                    "Compuerta OR",
                    "Construye un circuito donde el LED se encienda cuando al menos un interruptor esté encendido.",
                    "Y = A OR B",
                    "OR",
                    (a, b) => a || b),
                new PracticeExercise(
                    id: "not-01",
                    title: "Compuerta NOT",
                    objective: "Construye un circuito donde el LED se encienda cuando A esté apagado.",
                    logicConditionText: "Y = NOT A",
                    requiredGateType: "NOT",
                    expectedTruthTable: (a, b) => !a,
                    usesSingleInput: true,
                    tasks: PracticeTasks.ForGate(true)),
                Binary(
                    "nand-01",
                    "Compuerta NAND",
                    "Construye un circuito donde el LED se apague únicamente cuando A y B estén encendidos.",
                    "Y = A NAND B",
                    "NAND",
                    (a, b) => !(a && b)),
                Binary(
                    "nor-01",
                    "Compuerta NOR",
                    "Construye un circuito donde el LED se encienda solo cuando A y B estén apagados.",
                    "Y = A NOR B",
                    "NOR",
                    (a, b) => !(a || b)),
                Binary(
                    "xor-01",
                    "Compuerta XOR",
                    "Construye un circuito donde el LED se encienda cuando A y B sean distintos.",
                    "Y = A XOR B",
                    "XOR",
                    (a, b) => a != b),
                Binary(
                    "xnor-01",
                    "Compuerta XNOR",
                    "Construye un circuito donde el LED se encienda cuando A y B sean iguales.",
                    "Y = A XNOR B",
                    "XNOR",
                    (a, b) => a == b),
                Network(
                    "app-nand-01",
                    "NAND con AND y NOT",
                    "Construye una NAND con AND y NOT. El LED se apaga solo cuando A y B están encendidos.",
                    "Y = !(A AND B)",
                    "NOT",
                    (a, b) => !(a && b),
                    CircuitStructure.NandFromGates()),
                Network(
                    "app-nor-01",
                    "NOR con OR y NOT",
                    "Construye una NOR con OR y NOT. El LED se enciende solo cuando A y B están apagados.",
                    "Y = !(A OR B)",
                    "NOT",
                    (a, b) => !(a || b),
                    CircuitStructure.NorFromGates()),
                Network(
                    "app-branch-01",
                    "Señal compartida",
                    "Construye Y = (A AND B) OR (NOT A). El interruptor A entra a la AND y a la NOT.",
                    "Y = (A AND B) OR (NOT A)",
                    "OR",
                    (a, b) => (a && b) || !a,
                    CircuitStructure.AndOrNotBranch())
            }.AsReadOnly();
        }

        private static PracticeExercise Binary(
            string id,
            string title,
            string objective,
            string condition,
            string gate,
            System.Func<bool, bool, bool> table)
        {
            return new PracticeExercise(
                id: id,
                title: title,
                objective: objective,
                logicConditionText: condition,
                requiredGateType: gate,
                expectedTruthTable: table,
                tasks: PracticeTasks.ForGate(false));
        }

        private static PracticeExercise Network(
            string id,
            string title,
            string objective,
            string condition,
            string outputGate,
            System.Func<bool, bool, bool> table,
            CircuitStructure structure)
        {
            return new PracticeExercise(
                id: id,
                title: title,
                objective: objective,
                logicConditionText: condition,
                requiredGateType: outputGate,
                expectedTruthTable: table,
                initialGateType: structure.Modules[0].GateType,
                keepCompatibleWiring: false,
                moduleName: "Aplicación Real",
                structure: structure,
                tasks: PracticeTasks.ForNetwork());
        }
    }
}
