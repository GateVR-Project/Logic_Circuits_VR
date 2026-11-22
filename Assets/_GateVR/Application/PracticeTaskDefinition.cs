using System;
using System.Collections.Generic;

namespace GateVR.Application
{
    public enum TaskConditionKind
    {
        ReadRequirement,
        ConfirmGate,
        WireCircuit,
        TrySwitches,
        CheckResult
    }

    public enum LessonTaskState
    {
        Pending,
        Completed
    }

    /// <summary>
    /// Definición de una tarea. El estado no vive aquí: lo calcula <see cref="LessonProgress"/>.
    /// </summary>
    public sealed class PracticeTaskDefinition
    {
        public PracticeTaskDefinition(
            string id,
            string title,
            string description,
            int order,
            TaskConditionKind condition,
            params string[] dependsOn)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("La tarea necesita un id.", nameof(id));

            if (order < 1)
                throw new ArgumentOutOfRangeException(nameof(order), "El orden empieza en 1.");

            Id = id;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Order = order;
            Condition = condition;

            var dependencies = new List<string>();
            if (dependsOn != null)
            {
                for (int i = 0; i < dependsOn.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(dependsOn[i]))
                        dependencies.Add(dependsOn[i]);
                }
            }

            DependsOn = dependencies.AsReadOnly();
        }

        public string Id { get; private set; }

        public string Title { get; private set; }

        public string Description { get; private set; }

        public int Order { get; private set; }

        public TaskConditionKind Condition { get; private set; }

        public IReadOnlyList<string> DependsOn { get; private set; }

        public static void ValidateSet(IReadOnlyList<PracticeTaskDefinition> tasks)
        {
            if (tasks == null || tasks.Count < 5)
                throw new ArgumentException("Cada práctica necesita al menos cinco tareas.", nameof(tasks));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < tasks.Count; i++)
            {
                PracticeTaskDefinition task = tasks[i];
                if (task == null)
                    throw new ArgumentException("La lista de tareas tiene un elemento vacío.", nameof(tasks));

                if (!ids.Add(task.Id))
                    throw new ArgumentException("La tarea '" + task.Id + "' está repetida.", nameof(tasks));
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                IReadOnlyList<string> dependencies = tasks[i].DependsOn;
                for (int d = 0; d < dependencies.Count; d++)
                {
                    if (!ids.Contains(dependencies[d]))
                        throw new ArgumentException(
                            "La tarea '" + tasks[i].Id + "' depende de '" + dependencies[d] + "', que no existe.",
                            nameof(tasks));
                }
            }

            if (HasCycle(tasks))
                throw new ArgumentException("Las dependencias de las tareas forman un ciclo.", nameof(tasks));
        }

        private static bool HasCycle(IReadOnlyList<PracticeTaskDefinition> tasks)
        {
            var remaining = new Dictionary<string, int>(StringComparer.Ordinal);
            var dependents = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            for (int i = 0; i < tasks.Count; i++)
            {
                remaining[tasks[i].Id] = tasks[i].DependsOn.Count;
                dependents[tasks[i].Id] = new List<string>();
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                IReadOnlyList<string> dependencies = tasks[i].DependsOn;
                for (int d = 0; d < dependencies.Count; d++)
                    dependents[dependencies[d]].Add(tasks[i].Id);
            }

            var ready = new Queue<string>();
            foreach (KeyValuePair<string, int> pair in remaining)
            {
                if (pair.Value == 0)
                    ready.Enqueue(pair.Key);
            }

            int visited = 0;
            while (ready.Count > 0)
            {
                string id = ready.Dequeue();
                visited++;
                List<string> next = dependents[id];
                for (int i = 0; i < next.Count; i++)
                {
                    remaining[next[i]]--;
                    if (remaining[next[i]] == 0)
                        ready.Enqueue(next[i]);
                }
            }

            return visited != tasks.Count;
        }
    }

    /// <summary>
    /// Estado calculado de una tarea, listo para la interfaz o un registro posterior.
    /// </summary>
    public sealed class TaskStatus
    {
        public TaskStatus(PracticeTaskDefinition definition, LessonTaskState state)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            Id = definition.Id;
            Title = definition.Title;
            Description = definition.Description;
            Order = definition.Order;
            Condition = definition.Condition;
            State = state;
        }

        public string Id { get; private set; }

        public string Title { get; private set; }

        public string Description { get; private set; }

        public int Order { get; private set; }

        public TaskConditionKind Condition { get; private set; }

        public LessonTaskState State { get; private set; }
    }

    /// <summary>
    /// Tareas de las lecciones introductorias. Otra práctica puede aportar su propia lista.
    /// </summary>
    public static class PracticeTasks
    {
        public static IReadOnlyList<PracticeTaskDefinition> ForGate(string gateName, bool singleInput)
        {
            string wire = singleInput
                ? "Conecta el interruptor A a la entrada A y la salida de la compuerta al LED."
                : "Conecta los interruptores A y B a la compuerta y la salida al LED.";

            return new List<PracticeTaskDefinition>
            {
                new PracticeTaskDefinition(
                    "read",
                    "Leer el requerimiento",
                    "Pulsa Entendido cuando hayas leído el objetivo.",
                    1,
                    TaskConditionKind.ReadRequirement),
                new PracticeTaskDefinition(
                    "gate",
                    "Confirmar la compuerta",
                    "Pulsa " + gateName + " para confirmar la compuerta de esta práctica.",
                    2,
                    TaskConditionKind.ConfirmGate,
                    "read"),
                new PracticeTaskDefinition(
                    "wire",
                    "Cablear el circuito",
                    wire,
                    3,
                    TaskConditionKind.WireCircuit,
                    "gate"),
                new PracticeTaskDefinition(
                    "try",
                    "Probar los interruptores",
                    "Cambia los interruptores con el controlador y observa el LED.",
                    4,
                    TaskConditionKind.TrySwitches,
                    "wire"),
                new PracticeTaskDefinition(
                    "check",
                    "Comprobar el resultado",
                    "Pulsa Comprobar y consigue un circuito correcto.",
                    5,
                    TaskConditionKind.CheckResult,
                    "try")
            }.AsReadOnly();
        }
    }
}
