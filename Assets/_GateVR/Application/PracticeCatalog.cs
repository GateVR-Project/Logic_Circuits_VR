using System.Collections.Generic;

namespace GateVR.Application
{
    /// <summary>
    /// Catálogo de Fundamentos, Aplicación Real y Diagnóstico.
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
                    CircuitStructure.AndOrNotBranch()),
                new PracticeExercise(
                    id: "diag-gate-01",
                    title: "Compuerta incorrecta",
                    objective: "El LED debe encenderse solo cuando A y B estén encendidos. El chip instalado no cumple esa condición.",
                    logicConditionText: "Y = A AND B",
                    requiredGateType: "AND",
                    expectedTruthTable: (a, b) => a && b,
                    initialGateType: "OR",
                    keepCompatibleWiring: true,
                    moduleName: "Diagnóstico",
                    preset: new StationPreset(true, false, true),
                    tasks: PracticeTasks.ForWrongGate()),
                new PracticeExercise(
                    id: "diag-wire-01",
                    title: "Conexión faltante",
                    objective: "El LED debe encenderse si A o B están encendidos. Falta una conexión necesaria.",
                    logicConditionText: "Y = A OR B",
                    requiredGateType: "OR",
                    expectedTruthTable: (a, b) => a || b,
                    initialGateType: "OR",
                    keepCompatibleWiring: true,
                    moduleName: "Diagnóstico",
                    preset: new StationPreset(true, false, true, "m1", "B"),
                    tasks: PracticeTasks.ForGate(false)),
                new PracticeExercise(
                    id: "diag-net-01",
                    title: "NAND averiada",
                    objective: "Esta red debe apagar el LED solo cuando A y B estén encendidos. Un módulo no corresponde.",
                    logicConditionText: "Y = !(A AND B)",
                    requiredGateType: "NOT",
                    expectedTruthTable: (a, b) => !(a && b),
                    initialGateType: "AND",
                    keepCompatibleWiring: false,
                    moduleName: "Diagnóstico",
                    structure: CircuitStructure.NandFromGates(),
                    initialModuleTypes: new[] { "AND", "OR" },
                    preset: new StationPreset(true, true, true),
                    tasks: PracticeTasks.ForRepair())
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
